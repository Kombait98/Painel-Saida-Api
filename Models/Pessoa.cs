using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PainelSaida.Api.Models;

[Table("pessoa")]
public class Pessoa
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("nome_completo")]
    public string NomeCompleto { get; set; } = string.Empty;

    [MaxLength(14)]
    [Column("cpf")]
    public string? Cpf { get; set; }

    [Column("data_nascimento", TypeName = "date")]
    public DateTime? DataNascimento { get; set; }

    [Column("foto")]
    public string? Foto { get; set; }

    [Column("tipo_pessoa_id")]
    public int TipoPessoaId { get; set; }

    [ForeignKey(nameof(TipoPessoaId))]
    public TipoPessoa? TipoPessoa { get; set; }

    [MaxLength(100)]
    [Column("user_id")]
    public string? UserId { get; set; }

    [MaxLength(100)]
    [Column("senha")]
    public string? Senha { get; set; }

    [MaxLength(20)]
    [Column("telefone")]
    public string? Telefone { get; set; }

    [Column("ativo")]
    public bool Ativo { get; set; } = true;
}
