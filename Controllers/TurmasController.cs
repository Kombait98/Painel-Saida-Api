using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PainelSaida.Api.Data;
using PainelSaida.Api.Models;

namespace PainelSaida.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TurmasController : ControllerBase
{
    private readonly AppDbContext _context;

    public TurmasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Turma>>> GetTurmas()
    {
        var turmas = await _context.Turmas
            .Include(t => t.Turno)
            .Include(t => t.ProfessorPessoa)
            .Where(t => t.Ativo)
            .OrderBy(t => t.Nome)
            .ToListAsync();

        return Ok(turmas);
    }

    [HttpGet("painel-alunos")]
    public async Task<ActionResult<IEnumerable<PainelAlunoStatusDto>>> GetTodosAlunosPainel()
    {
        var alunos = await _context.Alunos
            .Include(a => a.Pessoa)
            .Include(a => a.Turma)
            .Include(a => a.Responsaveis)
                .ThenInclude(r => r.ResponsavelPessoa)
            .Include(a => a.Responsaveis)
                .ThenInclude(r => r.TipoVinculo)
            .Where(a => a.Ativo)
            .OrderBy(a => a.Id)
            .ToListAsync();

        var hojeUtc = DateTime.UtcNow.Date;

        var solicitacoesAtivas = await _context.SolicitacoesSaida
            .Include(s => s.Status)
            .Include(s => s.ResponsavelPessoa)
            .Where(s => s.Ativo && s.DataHoraSolicitacao >= hojeUtc && s.StatusId != 5 && s.StatusId != 6)
            .OrderByDescending(s => s.DataHoraSolicitacao)
            .ToListAsync();

        var historicosRecentes = await _context.HistoricosSaida
            .OrderByDescending(h => h.DataHora)
            .ToListAsync();

        var resultado = alunos.Select(a =>
        {
            var sol = solicitacoesAtivas.FirstOrDefault(s => s.AlunoId == a.Id);
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

            var responsavelPrincipal = responsaveisAtivos.FirstOrDefault()?.NomeResponsavel;

            var ultimoHistorico = sol != null
                ? historicosRecentes.FirstOrDefault(h => h.SolicitacaoSaidaId == sol.Id)?.DataHora
                : null;

            var ultimaData = ultimoHistorico
                             ?? sol?.DataHoraEntrega
                             ?? sol?.DataHoraConfirmacaoProfessor
                             ?? sol?.DataHoraSolicitacao
                             ?? DateTime.UtcNow;

            return new PainelAlunoStatusDto(
                AlunoId: a.Id,
                Matricula: a.Matricula,
                NomeAluno: a.Pessoa?.NomeCompleto ?? string.Empty,
                TurmaId: a.TurmaId,
                NomeTurma: a.Turma?.Nome ?? string.Empty,
                Sala: a.Turma?.Sala,
                PodeSairSozinho: a.PodeSairSozinho,
                ObservacaoPortaria: a.ObservacaoPortaria,
                SolicitacaoSaidaId: sol?.Id,
                StatusId: sol?.StatusId ?? 0,
                StatusNome: sol?.Status?.Nome ?? "NaSala",
                LastUpdatedAt: new DateTimeOffset(DateTime.SpecifyKind(ultimaData, DateTimeKind.Utc)).ToUnixTimeMilliseconds(),
                ResponsavelSolicitacaoId: sol?.ResponsavelPessoaId,
                NomeResponsavel: sol?.ResponsavelPessoa?.NomeCompleto ?? responsavelPrincipal,
                Responsaveis: responsaveisAtivos
            );
        });

        return Ok(resultado);
    }

    [HttpGet("{id:int}/alunos")]
    public async Task<ActionResult<IEnumerable<PainelAlunoStatusDto>>> GetAlunosComStatusPorTurma(int id)
    {
        var todos = await GetTodosAlunosPainel();
        if (todos.Result is OkObjectResult ok && ok.Value is IEnumerable<PainelAlunoStatusDto> lista)
        {
            return Ok(lista.Where(a => a.TurmaId == id));
        }
        return Ok(Array.Empty<PainelAlunoStatusDto>());
    }

    [HttpGet("turnos")]
    public async Task<ActionResult<IEnumerable<Turno>>> GetTurnos()
    {
        return Ok(await _context.Turnos.OrderBy(t => t.Id).ToListAsync());
    }

    [HttpGet("professores")]
    public async Task<ActionResult<IEnumerable<object>>> GetProfessores()
    {
        var professores = await _context.Pessoas
            .Where(p => p.Ativo && p.TipoPessoaId == 3)
            .OrderBy(p => p.NomeCompleto)
            .Select(p => new { p.Id, p.NomeCompleto, p.UserId })
            .ToListAsync();

        return Ok(professores);
    }

    [HttpPost]
    public async Task<ActionResult<Turma>> CriarTurma([FromBody] SalvarTurmaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            return BadRequest(new { message = "O nome da turma é obrigatório." });
        }

        var novaTurma = new Turma
        {
            Nome = dto.Nome.Trim(),
            AnoLetivo = dto.AnoLetivo > 0 ? dto.AnoLetivo : DateTime.UtcNow.Year,
            TurnoId = dto.TurnoId > 0 ? dto.TurnoId : 1,
            ProfessorPessoaId = dto.ProfessorPessoaId,
            Sala = string.IsNullOrWhiteSpace(dto.Sala) ? null : dto.Sala.Trim(),
            Ativo = true
        };

        _context.Turmas.Add(novaTurma);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTurmas), new { id = novaTurma.Id }, novaTurma);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Turma>> AtualizarTurma(int id, [FromBody] SalvarTurmaDto dto)
    {
        var turma = await _context.Turmas.FirstOrDefaultAsync(t => t.Id == id && t.Ativo);
        if (turma == null)
        {
            return NotFound(new { message = "Turma não encontrada." });
        }

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            return BadRequest(new { message = "O nome da turma é obrigatório." });
        }

        turma.Nome = dto.Nome.Trim();
        turma.AnoLetivo = dto.AnoLetivo > 0 ? dto.AnoLetivo : turma.AnoLetivo;
        turma.TurnoId = dto.TurnoId > 0 ? dto.TurnoId : turma.TurnoId;
        turma.ProfessorPessoaId = dto.ProfessorPessoaId;
        turma.Sala = string.IsNullOrWhiteSpace(dto.Sala) ? null : dto.Sala.Trim();

        await _context.SaveChangesAsync();

        return Ok(turma);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> ExcluirTurma(int id)
    {
        var turma = await _context.Turmas.FirstOrDefaultAsync(t => t.Id == id && t.Ativo);
        if (turma == null)
        {
            return NotFound(new { message = "Turma não encontrada." });
        }

        turma.Ativo = false;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Turma removida com sucesso." });
    }
}
