using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelSaida.Api.Data;
using PainelSaida.Api.Models;

namespace PainelSaida.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlunosController : ControllerBase
{
    private readonly AppDbContext _context;

    public AlunosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlunoCadastroResumoDto>>> GetAlunos([FromQuery] int? turmaId = null)
    {
        var query = _context.Alunos
            .Include(a => a.Pessoa)
            .Include(a => a.Turma)
            .Include(a => a.Responsaveis)
                .ThenInclude(r => r.ResponsavelPessoa)
            .Include(a => a.Responsaveis)
                .ThenInclude(r => r.TipoVinculo)
            .Where(a => a.Ativo)
            .AsQueryable();

        if (turmaId.HasValue)
        {
            query = query.Where(a => a.TurmaId == turmaId.Value);
        }

        var alunos = await query
            .OrderBy(a => a.Turma!.Nome)
            .ThenBy(a => a.Pessoa!.NomeCompleto)
            .ToListAsync();

        var resultado = alunos.Select(a =>
        {
            var responsaveisAtivos = a.Responsaveis
                .Where(r => r.Ativo)
                .OrderByDescending(r => r.ResponsavelPrincipal)
                .Select(r => new AlunoResponsavelDetalheDto(
                    r.Id,
                    r.AlunoId,
                    r.ResponsavelPessoaId,
                    r.ResponsavelPessoa?.NomeCompleto ?? string.Empty,
                    r.ResponsavelPessoa?.Cpf,
                    r.ResponsavelPessoa?.Telefone,
                    r.TipoVinculoId,
                    r.TipoVinculo?.Nome ?? string.Empty,
                    r.ResponsavelPrincipal,
                    r.PodeRetirar,
                    r.DataValidade
                ))
                .ToList();

            var respVinculo = responsaveisAtivos.FirstOrDefault();

            return new AlunoCadastroResumoDto(
                AlunoId: a.Id,
                PessoaId: a.PessoaId,
                NomeCompleto: a.Pessoa?.NomeCompleto ?? string.Empty,
                Cpf: a.Pessoa?.Cpf,
                DataNascimento: a.Pessoa?.DataNascimento,
                Matricula: a.Matricula,
                TurmaId: a.TurmaId,
                NomeTurma: a.Turma?.Nome ?? string.Empty,
                Sala: a.Turma?.Sala,
                PodeSairSozinho: a.PodeSairSozinho,
                ObservacaoPortaria: a.ObservacaoPortaria,
                NomeResponsavel: respVinculo?.NomeResponsavel,
                TelefoneResponsavel: respVinculo?.TelefoneResponsavel,
                TipoVinculoId: respVinculo?.TipoVinculoId,
                TipoVinculoNome: respVinculo?.TipoVinculoNome,
                Responsaveis: responsaveisAtivos
            );
        });

        return Ok(resultado);
    }

    [HttpGet("tipos-vinculo")]
    public async Task<ActionResult<IEnumerable<TipoVinculo>>> GetTiposVinculo()
    {
        return Ok(await _context.TiposVinculo.OrderBy(v => v.Id).ToListAsync());
    }

    [HttpGet("responsaveis-pessoas")]
    public async Task<ActionResult<IEnumerable<object>>> GetPessoasResponsaveis()
    {
        var responsaveis = await _context.Pessoas
            .Where(p => p.Ativo && p.TipoPessoaId == 2)
            .OrderBy(p => p.NomeCompleto)
            .Select(p => new { p.Id, p.NomeCompleto, p.Cpf, p.Telefone })
            .ToListAsync();

        return Ok(responsaveis);
    }

    [HttpPost]
    public async Task<ActionResult> CriarAluno([FromBody] SalvarAlunoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NomeCompleto) || string.IsNullOrWhiteSpace(dto.Matricula))
        {
            return BadRequest(new { message = "Nome completo e matrícula são obrigatórios." });
        }

        var matriculaNormalizada = dto.Matricula.Trim();
        var matriculaExiste = await _context.Alunos
            .AnyAsync(a => a.Matricula.ToLower() == matriculaNormalizada.ToLower());

        if (matriculaExiste)
        {
            return Conflict(new { message = $"A matrícula '{matriculaNormalizada}' já está em uso." });
        }

        var turma = await _context.Turmas.FirstOrDefaultAsync(t => t.Id == dto.TurmaId && t.Ativo);
        if (turma == null)
        {
            return BadRequest(new { message = "Selecione uma turma válida." });
        }

        // 1. Cria a Pessoa do Aluno (TipoPessoaId = 1)
        var pessoaAluno = new Pessoa
        {
            NomeCompleto = dto.NomeCompleto.Trim(),
            Cpf = string.IsNullOrWhiteSpace(dto.Cpf) ? null : dto.Cpf.Trim(),
            DataNascimento = dto.DataNascimento,
            TipoPessoaId = 1, // Aluno
            Ativo = true
        };

        _context.Pessoas.Add(pessoaAluno);
        await _context.SaveChangesAsync();

        // 2. Cria o registro de Aluno
        var novoAluno = new Aluno
        {
            PessoaId = pessoaAluno.Id,
            Matricula = matriculaNormalizada,
            TurmaId = dto.TurmaId,
            PodeSairSozinho = dto.PodeSairSozinho,
            ObservacaoPortaria = string.IsNullOrWhiteSpace(dto.ObservacaoPortaria) ? null : dto.ObservacaoPortaria.Trim(),
            Ativo = true
        };

        _context.Alunos.Add(novoAluno);
        await _context.SaveChangesAsync();

        // 3. Se informou responsável, cria Pessoa (Responsavel = 2) e AlunoResponsavel
        if (!string.IsNullOrWhiteSpace(dto.NomeResponsavel))
        {
            var pessoaResp = new Pessoa
            {
                NomeCompleto = dto.NomeResponsavel.Trim(),
                Telefone = string.IsNullOrWhiteSpace(dto.TelefoneResponsavel) ? null : dto.TelefoneResponsavel.Trim(),
                TipoPessoaId = 2, // Responsável
                Ativo = true
            };
            _context.Pessoas.Add(pessoaResp);
            await _context.SaveChangesAsync();

            var vinculo = new AlunoResponsavel
            {
                AlunoId = novoAluno.Id,
                ResponsavelPessoaId = pessoaResp.Id,
                TipoVinculoId = dto.TipoVinculoId is > 0 ? dto.TipoVinculoId.Value : 1,
                ResponsavelPrincipal = true,
                PodeRetirar = true,
                Ativo = true
            };
            _context.AlunosResponsaveis.Add(vinculo);
            await _context.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetAlunos), new { id = novoAluno.Id }, new { id = novoAluno.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> AtualizarAluno(int id, [FromBody] SalvarAlunoDto dto)
    {
        var aluno = await _context.Alunos
            .Include(a => a.Pessoa)
            .Include(a => a.Responsaveis)
                .ThenInclude(r => r.ResponsavelPessoa)
            .FirstOrDefaultAsync(a => a.Id == id && a.Ativo);

        if (aluno == null)
        {
            return NotFound(new { message = "Aluno não encontrado." });
        }

        if (string.IsNullOrWhiteSpace(dto.NomeCompleto) || string.IsNullOrWhiteSpace(dto.Matricula))
        {
            return BadRequest(new { message = "Nome completo e matrícula são obrigatórios." });
        }

        var matriculaNormalizada = dto.Matricula.Trim();
        var matriculaDuplicada = await _context.Alunos
            .AnyAsync(a => a.Id != id && a.Matricula.ToLower() == matriculaNormalizada.ToLower());

        if (matriculaDuplicada)
        {
            return Conflict(new { message = $"A matrícula '{matriculaNormalizada}' já pertence a outro aluno." });
        }

        if (aluno.Pessoa != null)
        {
            aluno.Pessoa.NomeCompleto = dto.NomeCompleto.Trim();
            aluno.Pessoa.Cpf = string.IsNullOrWhiteSpace(dto.Cpf) ? null : dto.Cpf.Trim();
            aluno.Pessoa.DataNascimento = dto.DataNascimento;
        }

        aluno.Matricula = matriculaNormalizada;
        aluno.TurmaId = dto.TurmaId;
        aluno.PodeSairSozinho = dto.PodeSairSozinho;
        aluno.ObservacaoPortaria = string.IsNullOrWhiteSpace(dto.ObservacaoPortaria) ? null : dto.ObservacaoPortaria.Trim();

        var respVinculo = aluno.Responsaveis.FirstOrDefault(r => r.Ativo && r.ResponsavelPrincipal)
                          ?? aluno.Responsaveis.FirstOrDefault(r => r.Ativo);

        if (!string.IsNullOrWhiteSpace(dto.NomeResponsavel))
        {
            if (respVinculo?.ResponsavelPessoa != null)
            {
                respVinculo.ResponsavelPessoa.NomeCompleto = dto.NomeResponsavel.Trim();
                respVinculo.ResponsavelPessoa.Telefone = string.IsNullOrWhiteSpace(dto.TelefoneResponsavel) ? null : dto.TelefoneResponsavel.Trim();
                if (dto.TipoVinculoId is > 0)
                {
                    respVinculo.TipoVinculoId = dto.TipoVinculoId.Value;
                }
            }
            else
            {
                var pessoaResp = new Pessoa
                {
                    NomeCompleto = dto.NomeResponsavel.Trim(),
                    Telefone = string.IsNullOrWhiteSpace(dto.TelefoneResponsavel) ? null : dto.TelefoneResponsavel.Trim(),
                    TipoPessoaId = 2,
                    Ativo = true
                };
                _context.Pessoas.Add(pessoaResp);
                await _context.SaveChangesAsync();

                _context.AlunosResponsaveis.Add(new AlunoResponsavel
                {
                    AlunoId = aluno.Id,
                    ResponsavelPessoaId = pessoaResp.Id,
                    TipoVinculoId = dto.TipoVinculoId is > 0 ? dto.TipoVinculoId.Value : 1,
                    ResponsavelPrincipal = true,
                    PodeRetirar = true,
                    Ativo = true
                });
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Aluno atualizado com sucesso!" });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> ExcluirAluno(int id)
    {
        var aluno = await _context.Alunos.FirstOrDefaultAsync(a => a.Id == id && a.Ativo);
        if (aluno == null)
        {
            return NotFound(new { message = "Aluno não encontrado." });
        }

        aluno.Ativo = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Aluno removido com sucesso." });
    }

    // ========================================================================
    // Endpoints de Gestão da Relação Aluno <-> Responsável (aluno_responsavel)
    // ========================================================================

    [HttpPost("{alunoId:int}/responsaveis")]
    public async Task<ActionResult> AdicionarResponsavelAoAluno(int alunoId, [FromBody] VincularResponsavelDto dto)
    {
        var aluno = await _context.Alunos
            .Include(a => a.Responsaveis)
            .FirstOrDefaultAsync(a => a.Id == alunoId && a.Ativo);

        if (aluno == null)
        {
            return NotFound(new { message = "Aluno não encontrado." });
        }

        int responsavelPessoaId;

        if (dto.ResponsavelPessoaId is > 0)
        {
            var pessoaExistente = await _context.Pessoas.FindAsync(dto.ResponsavelPessoaId.Value);
            if (pessoaExistente == null)
            {
                return BadRequest(new { message = "Responsável selecionado não encontrado." });
            }
            responsavelPessoaId = pessoaExistente.Id;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.NomeCompleto))
            {
                return BadRequest(new { message = "Informe o nome do responsável ou selecione um existente." });
            }

            var novaPessoaResp = new Pessoa
            {
                NomeCompleto = dto.NomeCompleto.Trim(),
                Cpf = string.IsNullOrWhiteSpace(dto.Cpf) ? null : dto.Cpf.Trim(),
                Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim(),
                TipoPessoaId = 2, // Responsavel
                Ativo = true
            };
            _context.Pessoas.Add(novaPessoaResp);
            await _context.SaveChangesAsync();
            responsavelPessoaId = novaPessoaResp.Id;
        }

        // Se marcou como principal, desmarca os outros desse aluno
        if (dto.ResponsavelPrincipal)
        {
            foreach (var r in aluno.Responsaveis.Where(r => r.Ativo))
            {
                r.ResponsavelPrincipal = false;
            }
        }

        var novoVinculo = new AlunoResponsavel
        {
            AlunoId = alunoId,
            ResponsavelPessoaId = responsavelPessoaId,
            TipoVinculoId = dto.TipoVinculoId > 0 ? dto.TipoVinculoId : 1,
            ResponsavelPrincipal = dto.ResponsavelPrincipal,
            PodeRetirar = dto.PodeRetirar,
            DataValidade = dto.DataValidade,
            Ativo = true
        };

        _context.AlunosResponsaveis.Add(novoVinculo);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Responsável vinculado ao aluno com sucesso!", id = novoVinculo.Id });
    }

    [HttpPatch("responsaveis/{vinculoId:int}/toggle-retirar")]
    public async Task<ActionResult> AlternarPermissaoRetirada(int vinculoId)
    {
        var vinculo = await _context.AlunosResponsaveis.FirstOrDefaultAsync(v => v.Id == vinculoId && v.Ativo);
        if (vinculo == null)
        {
            return NotFound(new { message = "Vínculo de responsável não encontrado." });
        }

        vinculo.PodeRetirar = !vinculo.PodeRetirar;
        await _context.SaveChangesAsync();

        return Ok(new { vinculo.Id, vinculo.PodeRetirar });
    }

    [HttpDelete("responsaveis/{vinculoId:int}")]
    public async Task<ActionResult> RemoverVinculoResponsavel(int vinculoId)
    {
        var vinculo = await _context.AlunosResponsaveis.FirstOrDefaultAsync(v => v.Id == vinculoId && v.Ativo);
        if (vinculo == null)
        {
            return NotFound(new { message = "Vínculo de responsável não encontrado." });
        }

        vinculo.Ativo = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Vínculo removido com sucesso." });
    }
}
