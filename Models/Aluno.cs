using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PainelSaida.Api.Models;

[Table("aluno")]
public class Aluno
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("pessoa_id")]
    public int PessoaId { get; set; }

    [ForeignKey(nameof(PessoaId))]
    public Pessoa? Pessoa { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("matricula")]
    public string Matricula { get; set; } = string.Empty;

    [Column("turma_id")]
    public int TurmaId { get; set; }

    [ForeignKey(nameof(TurmaId))]
    public Turma? Turma { get; set; }

    [Column("pode_sair_sozinho")]
    public bool PodeSairSozinho { get; set; }

    [Column("observacao_portaria")]
    public string? ObservacaoPortaria { get; set; }

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    public ICollection<AlunoResponsavel> Responsaveis { get; set; } = new List<AlunoResponsavel>();
}

[Table("aluno_responsavel")]
public class AlunoResponsavel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("aluno_id")]
    public int AlunoId { get; set; }

    [ForeignKey(nameof(AlunoId))]
    public Aluno? Aluno { get; set; }

    [Column("responsavel_pessoa_id")]
    public int ResponsavelPessoaId { get; set; }

    [ForeignKey(nameof(ResponsavelPessoaId))]
    public Pessoa? ResponsavelPessoa { get; set; }

    [Column("tipo_vinculo_id")]
    public int TipoVinculoId { get; set; }

    [ForeignKey(nameof(TipoVinculoId))]
    public TipoVinculo? TipoVinculo { get; set; }

    [Column("responsavel_principal")]
    public bool ResponsavelPrincipal { get; set; }

    [Column("pode_retirar")]
    public bool PodeRetirar { get; set; } = true;

    [Column("data_validade")]
    public DateTime? DataValidade { get; set; }

    [Column("ativo")]
    public bool Ativo { get; set; } = true;
}
