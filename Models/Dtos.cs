namespace PainelSaida.Api.Models;

// DTOs de Autenticação e Gestão de Usuários (baseados em Pessoa + TipoPessoa)
public record LoginRequestDto(string UserId, string Senha);

public record LoginResponseDto(
    int PessoaId,
    string UserId,
    string NomeCompleto,
    int TipoPessoaId,
    string TipoPessoaNome,
    int? TurmaId
);

public record RegistrarUsuarioDto(
    string NomeCompleto,
    string? Cpf,
    DateTime? DataNascimento,
    string? Telefone,
    int TipoPessoaId,
    string UserId,
    string Senha,
    int? TurmaId
);

public record ResetSenhaDto(
    string UserId,
    string? Cpf,
    string NovaSenha
);

public record UsuarioResumoDto(
    int PessoaId,
    string NomeCompleto,
    string? Cpf,
    string? Telefone,
    string UserId,
    int TipoPessoaId,
    string TipoPessoaNome,
    int? TurmaId,
    string? NomeTurma
);

// DTOs de Vínculo Aluno <-> Responsável (tabela aluno_responsavel)
public record AlunoResponsavelDetalheDto(
    int Id,
    int AlunoId,
    int ResponsavelPessoaId,
    string NomeResponsavel,
    string? CpfResponsavel,
    string? TelefoneResponsavel,
    int TipoVinculoId,
    string TipoVinculoNome,
    bool ResponsavelPrincipal,
    bool PodeRetirar,
    DateTime? DataValidade
);

public record VincularResponsavelDto(
    int? ResponsavelPessoaId,
    string? NomeCompleto,
    string? Cpf,
    string? Telefone,
    int TipoVinculoId,
    bool ResponsavelPrincipal,
    bool PodeRetirar,
    DateTime? DataValidade
);

// DTOs de Solicitação de Saída e Painel
public record CriarSolicitacaoSaidaDto(
    int AlunoId,
    int? ResponsavelPessoaId,
    int? PorteiroPessoaId,
    string? Observacao
);

public record AtualizarStatusSaidaDto(
    int StatusId,
    int? UsuarioPessoaId,
    string? AlteradoPorUserId,
    string? Observacao,
    int? ResponsavelPessoaId = null
);

public record PainelAlunoStatusDto(
    int AlunoId,
    string Matricula,
    string NomeAluno,
    int TurmaId,
    string NomeTurma,
    string? Sala,
    bool PodeSairSozinho,
    string? ObservacaoPortaria,
    int? SolicitacaoSaidaId,
    int StatusId,
    string StatusNome,
    long LastUpdatedAt,
    int? ResponsavelSolicitacaoId,
    string? NomeResponsavel,
    IReadOnlyList<AlunoResponsavelDetalheDto> Responsaveis
);

// DTOs de Cadastro e Edição de Turmas e Alunos
public record SalvarTurmaDto(
    string Nome,
    int AnoLetivo,
    int TurnoId,
    int? ProfessorPessoaId,
    string? Sala
);

public record SalvarAlunoDto(
    string NomeCompleto,
    string? Cpf,
    DateTime? DataNascimento,
    string Matricula,
    int TurmaId,
    bool PodeSairSozinho,
    string? ObservacaoPortaria,
    string? NomeResponsavel,
    string? TelefoneResponsavel,
    int? TipoVinculoId
);

public record AlunoCadastroResumoDto(
    int AlunoId,
    int PessoaId,
    string NomeCompleto,
    string? Cpf,
    DateTime? DataNascimento,
    string Matricula,
    int TurmaId,
    string NomeTurma,
    string? Sala,
    bool PodeSairSozinho,
    string? ObservacaoPortaria,
    string? NomeResponsavel,
    string? TelefoneResponsavel,
    int? TipoVinculoId,
    string? TipoVinculoNome,
    IReadOnlyList<AlunoResponsavelDetalheDto> Responsaveis
);
