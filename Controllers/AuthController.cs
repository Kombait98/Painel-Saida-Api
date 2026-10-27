using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelSaida.Api.Data;
using PainelSaida.Api.Models;

namespace PainelSaida.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var pessoa = await _context.Pessoas
            .Include(p => p.TipoPessoa)
            .FirstOrDefaultAsync(p =>
                p.Ativo &&
                p.UserId != null &&
                p.UserId.ToLower() == dto.UserId.ToLower() &&
                p.Senha == dto.Senha);

        if (pessoa == null)
        {
            return Unauthorized(new { message = "Usuário ou senha inválidos." });
        }

        // Caso seja professor, busca a turma vinculada a ele
        var turma = await _context.Turmas
            .FirstOrDefaultAsync(t => t.Ativo && t.ProfessorPessoaId == pessoa.Id);

        return Ok(new LoginResponseDto(
            pessoa.Id,
            pessoa.UserId!,
            pessoa.NomeCompleto,
            pessoa.TipoPessoaId,
            pessoa.TipoPessoa?.Nome ?? string.Empty,
            turma?.Id
        ));
    }

    [HttpGet("tipos-pessoa")]
    public async Task<ActionResult<IEnumerable<TipoPessoa>>> GetTiposPessoa()
    {
        var tipos = await _context.TiposPessoa
            .OrderBy(t => t.Id)
            .ToListAsync();
        return Ok(tipos);
    }

    [HttpGet("usuarios")]
    public async Task<ActionResult<IEnumerable<UsuarioResumoDto>>> GetUsuarios()
    {
        var pessoas = await _context.Pessoas
            .Include(p => p.TipoPessoa)
            .Where(p => p.Ativo && p.UserId != null && p.UserId != "")
            .OrderBy(p => p.Id)
            .ToListAsync();

        var turmas = await _context.Turmas
            .Where(t => t.Ativo)
            .ToListAsync();

        var resultado = pessoas.Select(p =>
        {
            var turma = turmas.FirstOrDefault(t => t.ProfessorPessoaId == p.Id);
            return new UsuarioResumoDto(
                p.Id,
                p.NomeCompleto,
                p.Cpf,
                p.Telefone,
                p.UserId!,
                p.TipoPessoaId,
                p.TipoPessoa?.Nome ?? string.Empty,
                turma?.Id,
                turma?.Nome
            );
        });

        return Ok(resultado);
    }

    [HttpPost("registrar")]
    public async Task<ActionResult<LoginResponseDto>> Registrar([FromBody] RegistrarUsuarioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NomeCompleto) ||
            string.IsNullOrWhiteSpace(dto.UserId) ||
            string.IsNullOrWhiteSpace(dto.Senha))
        {
            return BadRequest(new { message = "Nome completo, usuário e senha são obrigatórios." });
        }

        var userIdNormalizado = dto.UserId.Trim();
        var jaExiste = await _context.Pessoas
            .AnyAsync(p => p.UserId != null && p.UserId.ToLower() == userIdNormalizado.ToLower());

        if (jaExiste)
        {
            return Conflict(new { message = $"O usuário '{userIdNormalizado}' já está cadastrado." });
        }

        var tipoPessoa = await _context.TiposPessoa.FindAsync(dto.TipoPessoaId);
        if (tipoPessoa == null)
        {
            return BadRequest(new { message = "Tipo de pessoa inválido." });
        }

        var novaPessoa = new Pessoa
        {
            NomeCompleto = dto.NomeCompleto.Trim(),
            Cpf = string.IsNullOrWhiteSpace(dto.Cpf) ? null : dto.Cpf.Trim(),
            DataNascimento = dto.DataNascimento,
            Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim(),
            TipoPessoaId = dto.TipoPessoaId,
            UserId = userIdNormalizado,
            Senha = dto.Senha,
            Ativo = true
        };

        _context.Pessoas.Add(novaPessoa);
        await _context.SaveChangesAsync();

        // Se for Professor (TipoPessoaId == 3) e selecionou uma turma, vincula à turma
        Turma? turmaVinculada = null;
        if (dto.TipoPessoaId == 3 && dto.TurmaId.HasValue)
        {
            turmaVinculada = await _context.Turmas.FindAsync(dto.TurmaId.Value);
            if (turmaVinculada != null)
            {
                turmaVinculada.ProfessorPessoaId = novaPessoa.Id;
                await _context.SaveChangesAsync();
            }
        }

        return CreatedAtAction(nameof(GetUsuarios), new { id = novaPessoa.Id }, new LoginResponseDto(
            novaPessoa.Id,
            novaPessoa.UserId,
            novaPessoa.NomeCompleto,
            novaPessoa.TipoPessoaId,
            tipoPessoa.Nome,
            turmaVinculada?.Id
        ));
    }

    [HttpPut("usuarios/{id:int}")]
    public async Task<ActionResult<LoginResponseDto>> AtualizarUsuario(int id, [FromBody] RegistrarUsuarioDto dto)
    {
        var pessoa = await _context.Pessoas.FirstOrDefaultAsync(p => p.Id == id && p.Ativo);
        if (pessoa == null)
        {
            return NotFound(new { message = "Usuário não encontrado." });
        }

        if (string.IsNullOrWhiteSpace(dto.NomeCompleto) || string.IsNullOrWhiteSpace(dto.UserId))
        {
            return BadRequest(new { message = "Nome completo e usuário (login) são obrigatórios." });
        }

        var userIdNormalizado = dto.UserId.Trim();
        var duplicado = await _context.Pessoas
            .AnyAsync(p => p.Id != id && p.UserId != null && p.UserId.ToLower() == userIdNormalizado.ToLower());

        if (duplicado)
        {
            return Conflict(new { message = $"O login '{userIdNormalizado}' já pertence a outro usuário." });
        }

        var tipoPessoa = await _context.TiposPessoa.FindAsync(dto.TipoPessoaId);
        if (tipoPessoa == null)
        {
            return BadRequest(new { message = "Tipo de pessoa inválido." });
        }

        pessoa.NomeCompleto = dto.NomeCompleto.Trim();
        pessoa.Cpf = string.IsNullOrWhiteSpace(dto.Cpf) ? null : dto.Cpf.Trim();
        pessoa.DataNascimento = dto.DataNascimento ?? pessoa.DataNascimento;
        pessoa.Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim();
        pessoa.TipoPessoaId = dto.TipoPessoaId;
        pessoa.UserId = userIdNormalizado;

        if (!string.IsNullOrWhiteSpace(dto.Senha))
        {
            pessoa.Senha = dto.Senha;
        }

        await _context.SaveChangesAsync();

        // Atualiza vínculo de turma caso seja Professor
        var turmasAtuais = await _context.Turmas.Where(t => t.ProfessorPessoaId == pessoa.Id).ToListAsync();
        foreach (var t in turmasAtuais)
        {
            t.ProfessorPessoaId = null;
        }

        Turma? turmaVinculada = null;
        if (dto.TipoPessoaId == 3 && dto.TurmaId.HasValue)
        {
            turmaVinculada = await _context.Turmas.FindAsync(dto.TurmaId.Value);
            if (turmaVinculada != null)
            {
                turmaVinculada.ProfessorPessoaId = pessoa.Id;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new LoginResponseDto(
            pessoa.Id,
            pessoa.UserId!,
            pessoa.NomeCompleto,
            pessoa.TipoPessoaId,
            tipoPessoa.Nome,
            turmaVinculada?.Id
        ));
    }

    [HttpPost("reset-senha")]
    public async Task<ActionResult> ResetSenha([FromBody] ResetSenhaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserId) || string.IsNullOrWhiteSpace(dto.NovaSenha))
        {
            return BadRequest(new { message = "Informe o usuário e a nova senha." });
        }

        var pessoa = await _context.Pessoas
            .FirstOrDefaultAsync(p =>
                p.Ativo &&
                p.UserId != null &&
                p.UserId.ToLower() == dto.UserId.Trim().ToLower());

        if (pessoa == null)
        {
            return NotFound(new { message = "Usuário não encontrado." });
        }

        // Se o usuário enviou CPF para validação e a pessoa possui CPF cadastrado, valida se confere
        if (!string.IsNullOrWhiteSpace(dto.Cpf) &&
            !string.IsNullOrWhiteSpace(pessoa.Cpf) &&
            pessoa.Cpf.Replace(".", "").Replace("-", "").Trim() != dto.Cpf.Replace(".", "").Replace("-", "").Trim())
        {
            return BadRequest(new { message = "O CPF informado não confere com o cadastro deste usuário." });
        }

        pessoa.Senha = dto.NovaSenha;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Senha do usuário '{pessoa.UserId}' redefinida com sucesso!" });
    }
}
