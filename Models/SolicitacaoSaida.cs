using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PainelSaida.Api.Models;

[Table("solicitacao_saida")]
public class SolicitacaoSaida
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("aluno_id")]
    public int AlunoId { get; set; }

    [ForeignKey(nameof(AlunoId))]
    public Aluno? Aluno { get; set; }

    [Column("responsavel_pessoa_id")]
    public int? ResponsavelPessoaId { get; set; }

    [ForeignKey(nameof(ResponsavelPessoaId))]
    public Pessoa? ResponsavelPessoa { get; set; }

    [Column("porteiro_pessoa_id")]
    public int? PorteiroPessoaId { get; set; }

    [ForeignKey(nameof(PorteiroPessoaId))]
    public Pessoa? PorteiroPessoa { get; set; }

    [Column("professor_pessoa_id")]
    public int? ProfessorPessoaId { get; set; }

    [ForeignKey(nameof(ProfessorPessoaId))]
    public Pessoa? ProfessorPessoa { get; set; }

    [Column("data_hora_solicitacao")]
    public DateTime DataHoraSolicitacao { get; set; } = DateTime.UtcNow;

    [Column("data_hora_confirmacao_professor")]
    public DateTime? DataHoraConfirmacaoProfessor { get; set; }

    [Column("data_hora_entrega")]
    public DateTime? DataHoraEntrega { get; set; }

    [Column("status_id")]
    public int StatusId { get; set; }

    [ForeignKey(nameof(StatusId))]
    public StatusSaida? Status { get; set; }

    [Column("observacao")]
    public string? Observacao { get; set; }

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    public ICollection<HistoricoSaida> Historicos { get; set; } = new List<HistoricoSaida>();
}

[Table("historico_saida")]
public class HistoricoSaida
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("solicitacao_saida_id")]
    public int SolicitacaoSaidaId { get; set; }

    [ForeignKey(nameof(SolicitacaoSaidaId))]
    public SolicitacaoSaida? SolicitacaoSaida { get; set; }

    [Column("status_id")]
    public int StatusId { get; set; }

    [ForeignKey(nameof(StatusId))]
    public StatusSaida? Status { get; set; }

    [Column("data_hora")]
    public DateTime DataHora { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    [Column("alterado_por_user_id")]
    public string? AlteradoPorUserId { get; set; }

    [Column("observacao")]
    public string? Observacao { get; set; }
}
