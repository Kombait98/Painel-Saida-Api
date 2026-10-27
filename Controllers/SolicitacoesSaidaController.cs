using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PainelSaida.Api.Data;
using PainelSaida.Api.Hubs;
using PainelSaida.Api.Models;

namespace PainelSaida.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolicitacoesSaidaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<DismissalHub> _hubContext;

    public SolicitacoesSaidaController(AppDbContext context, IHubContext<DismissalHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SolicitacaoSaida>>> GetSolicitacoesAtivas([FromQuery] int? turmaId = null)
    {
        var hojeUtc = DateTime.UtcNow.Date;
        var query = _context.SolicitacoesSaida
            .Include(s => s.Aluno)
                .ThenInclude(a => a!.Pessoa)
            .Include(s => s.Aluno)
                .ThenInclude(a => a!.Turma)
            .Include(s => s.Status)
            .Include(s => s.ResponsavelPessoa)
            .Include(s => s.PorteiroPessoa)
            .Include(s => s.ProfessorPessoa)
            .Where(s => s.Ativo && s.DataHoraSolicitacao >= hojeUtc)
            .AsQueryable();

        if (turmaId.HasValue)
        {
            query = query.Where(s => s.Aluno!.TurmaId == turmaId.Value);
        }

        return Ok(await query.OrderByDescending(s => s.DataHoraSolicitacao).ToListAsync());
    }

    /// <summary>
    /// Porteiro solicita a saída do aluno (cria SolicitacaoSaida + primeiro registro em HistoricoSaida)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SolicitacaoSaida>> SolicitarSaida([FromBody] CriarSolicitacaoSaidaDto dto)
    {
        var aluno = await _context.Alunos
            .Include(a => a.Pessoa)
            .Include(a => a.Turma)
            .FirstOrDefaultAsync(a => a.Id == dto.AlunoId && a.Ativo);

        if (aluno == null)
        {
            return NotFound(new { message = "Aluno não encontrado." });
        }

        // Verifica se já existe uma solicitação ativa para este aluno HOJE
        var agora = DateTime.UtcNow;
        var hojeUtc = agora.Date;
        var solicitacaoExistente = await _context.SolicitacoesSaida
            .FirstOrDefaultAsync(s => s.AlunoId == dto.AlunoId && s.Ativo && s.DataHoraSolicitacao >= hojeUtc && s.StatusId != 4 && s.StatusId != 5);

        if (solicitacaoExistente != null)
        {
            // Se o porteiro está chamando novamente, atualiza a solicitação existente e gera histórico
            solicitacaoExistente.StatusId = 1; // AguardandoProfessor
            solicitacaoExistente.DataHoraSolicitacao = agora;
            solicitacaoExistente.Observacao = dto.Observacao ?? solicitacaoExistente.Observacao;

            _context.HistoricosSaida.Add(new HistoricoSaida
            {
                SolicitacaoSaidaId = solicitacaoExistente.Id,
                StatusId = 1,
                DataHora = agora,
                AlteradoPorUserId = dto.PorteiroPessoaId?.ToString(),
                Observacao = "Porteiro chamou o aluno novamente"
            });

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("SolicitacaoAtualizada", new
            {
                solicitacaoExistente.Id,
                solicitacaoExistente.AlunoId,
                aluno.TurmaId,
                solicitacaoExistente.StatusId
            });

            return Ok(solicitacaoExistente);
        }

        var novaSolicitacao = new SolicitacaoSaida
        {
            AlunoId = dto.AlunoId,
            ResponsavelPessoaId = dto.ResponsavelPessoaId,
            PorteiroPessoaId = dto.PorteiroPessoaId,
            ProfessorPessoaId = aluno.Turma?.ProfessorPessoaId,
            DataHoraSolicitacao = agora,
            StatusId = 1, // 1 = AguardandoProfessor
            Observacao = dto.Observacao,
            Ativo = true
        };

        _context.SolicitacoesSaida.Add(novaSolicitacao);
        await _context.SaveChangesAsync();

        var historicoInicial = new HistoricoSaida
        {
            SolicitacaoSaidaId = novaSolicitacao.Id,
            StatusId = 1,
            DataHora = agora,
            AlteradoPorUserId = dto.PorteiroPessoaId?.ToString(),
            Observacao = dto.Observacao ?? "Solicitação de saída aberta pela portaria"
        };

        _context.HistoricosSaida.Add(historicoInicial);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("SolicitacaoAtualizada", new
        {
            novaSolicitacao.Id,
            novaSolicitacao.AlunoId,
            aluno.TurmaId,
            novaSolicitacao.StatusId
        });

        return CreatedAtAction(nameof(GetSolicitacoesAtivas), new { id = novaSolicitacao.Id }, novaSolicitacao);
    }

    /// <summary>
    /// Professor ou Porteiro atualiza o status da Solicitação de Saída e grava no HistoricoSaida
    /// StatusId:
    /// 1 = AguardandoProfessor
    /// 2 = AlunoChamado (se arrumando / ciente do professor)
    /// 3 = AlunoNaPortaria (liberado da sala a caminho da portaria)
    /// 4 = Entregue (entregue ao responsável pela portaria)
    /// 5 = Cancelada
    /// 6 = Recusada
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<SolicitacaoSaida>> AtualizarStatus(int id, [FromBody] AtualizarStatusSaidaDto dto)
    {
        var solicitacao = await _context.SolicitacoesSaida
            .Include(s => s.Aluno)
            .Include(s => s.Status)
            .FirstOrDefaultAsync(s => s.Id == id && s.Ativo);

        if (solicitacao == null)
        {
            return NotFound(new { message = "Solicitação de saída não encontrada." });
        }

        var statusExiste = await _context.StatusSaidas.AnyAsync(st => st.Id == dto.StatusId);
        if (!statusExiste)
        {
            return BadRequest(new { message = "StatusId inválido." });
        }

        var agora = DateTime.UtcNow;
        solicitacao.StatusId = dto.StatusId;

        if (dto.StatusId == 2 || dto.StatusId == 3)
        {
            solicitacao.DataHoraConfirmacaoProfessor ??= agora;
            if (dto.UsuarioPessoaId.HasValue)
            {
                solicitacao.ProfessorPessoaId = dto.UsuarioPessoaId.Value;
            }
        }
        else if (dto.StatusId == 4)
        {
            solicitacao.DataHoraEntrega = agora;
            if (dto.ResponsavelPessoaId.HasValue)
            {
                solicitacao.ResponsavelPessoaId = dto.ResponsavelPessoaId.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.Observacao))
        {
            solicitacao.Observacao = dto.Observacao;
        }

        _context.HistoricosSaida.Add(new HistoricoSaida
        {
            SolicitacaoSaidaId = solicitacao.Id,
            StatusId = dto.StatusId,
            DataHora = agora,
            AlteradoPorUserId = dto.AlteradoPorUserId ?? dto.UsuarioPessoaId?.ToString(),
            Observacao = dto.Observacao
        });

        await _context.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("SolicitacaoAtualizada", new
        {
            solicitacao.Id,
            solicitacao.AlunoId,
            TurmaId = solicitacao.Aluno?.TurmaId,
            solicitacao.StatusId
        });

        return Ok(solicitacao);
    }

    [HttpGet("{id:int}/historico")]
    public async Task<ActionResult<IEnumerable<HistoricoSaida>>> GetHistorico(int id)
    {
        var historico = await _context.HistoricosSaida
            .Include(h => h.Status)
            .Where(h => h.SolicitacaoSaidaId == id)
            .OrderBy(h => h.DataHora)
            .ToListAsync();

        return Ok(historico);
    }

    [HttpPost("reset")]
    public async Task<ActionResult> ResetarSolicitacoes()
    {
        var ativas = await _context.SolicitacoesSaida
            .Where(s => s.Ativo)
            .ToListAsync();

        foreach (var sol in ativas)
        {
            sol.Ativo = false;
        }

        await _context.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync("SolicitacaoAtualizada", new { Reset = true });

        return Ok(new { message = "Todas as solicitações foram reiniciadas." });
    }
}
