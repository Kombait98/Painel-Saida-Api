using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PainelSaida.Api.Models;

[Table("turma")]
public class Turma
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("ano_letivo")]
    public int AnoLetivo { get; set; }

    [Column("turno_id")]
    public int TurnoId { get; set; }

    [ForeignKey(nameof(TurnoId))]
    public Turno? Turno { get; set; }

    [Column("professor_pessoa_id")]
    public int? ProfessorPessoaId { get; set; }

    [ForeignKey(nameof(ProfessorPessoaId))]
    public Pessoa? ProfessorPessoa { get; set; }

    [MaxLength(50)]
    [Column("sala")]
    public string? Sala { get; set; }

    [Column("ativo")]
    public bool Ativo { get; set; } = true;
}
