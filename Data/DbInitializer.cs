using Microsoft.EntityFrameworkCore;
using PainelSaida.Api.Models;

namespace PainelSaida.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // 1. TipoPessoa
        if (!await context.TiposPessoa.AnyAsync())
        {
            await context.TiposPessoa.AddRangeAsync(
                new TipoPessoa { Id = 1, Nome = "Aluno" },
                new TipoPessoa { Id = 2, Nome = "Responsavel" },
                new TipoPessoa { Id = 3, Nome = "Professor" },
                new TipoPessoa { Id = 4, Nome = "Porteiro" },
                new TipoPessoa { Id = 5, Nome = "Administrador" }
            );
            await context.SaveChangesAsync();
        }

        // 2. Turno
        if (!await context.Turnos.AnyAsync())
        {
            await context.Turnos.AddRangeAsync(
                new Turno { Id = 1, Nome = "Matutino" },
                new Turno { Id = 2, Nome = "Vespertino" },
                new Turno { Id = 3, Nome = "Integral" },
                new Turno { Id = 4, Nome = "Noturno" }
            );
            await context.SaveChangesAsync();
        }

        // 3. TipoVinculo
        if (!await context.TiposVinculo.AnyAsync())
        {
            await context.TiposVinculo.AddRangeAsync(
                new TipoVinculo { Id = 1, Nome = "Mae" },
                new TipoVinculo { Id = 2, Nome = "Pai" },
                new TipoVinculo { Id = 3, Nome = "TutorLegal" },
                new TipoVinculo { Id = 4, Nome = "Avo" },
                new TipoVinculo { Id = 5, Nome = "Familiar" },
                new TipoVinculo { Id = 6, Nome = "Outro" }
            );
            await context.SaveChangesAsync();
        }

        // 4. StatusSaida
        if (!await context.StatusSaidas.AnyAsync())
        {
            await context.StatusSaidas.AddRangeAsync(
                new StatusSaida { Id = 1, Nome = "AguardandoProfessor" },
                new StatusSaida { Id = 2, Nome = "AlunoChamado" },
                new StatusSaida { Id = 3, Nome = "AlunoNaPortaria" },
                new StatusSaida { Id = 4, Nome = "Entregue" },
                new StatusSaida { Id = 5, Nome = "Cancelada" },
                new StatusSaida { Id = 6, Nome = "Recusada" }
            );
            await context.SaveChangesAsync();
        }

        // 5. Pessoas (Porteiro, Professores, Alunos e Responsáveis iniciais)
        if (!await context.Pessoas.AnyAsync())
        {
            var pessoas = new List<Pessoa>
            {
                // Porteiro (Id = 1)
                new() { Id = 1, NomeCompleto = "Carlos Porteiro", Cpf = "111.111.111-11", TipoPessoaId = 4, UserId = "porteiro", Senha = "123", Telefone = "11999990001", Ativo = true },
                // Professores (Ids = 2..6)
                new() { Id = 2, NomeCompleto = "Profª. Mariana (Turma A)", Cpf = "222.222.222-01", TipoPessoaId = 3, UserId = "prof.a", Senha = "123", Telefone = "11999990002", Ativo = true },
                new() { Id = 3, NomeCompleto = "Prof. Roberto (Turma B)", Cpf = "222.222.222-02", TipoPessoaId = 3, UserId = "prof.b", Senha = "123", Telefone = "11999990003", Ativo = true },
                new() { Id = 4, NomeCompleto = "Profª. Camila (Turma C)", Cpf = "222.222.222-03", TipoPessoaId = 3, UserId = "prof.c", Senha = "123", Telefone = "11999990004", Ativo = true },
                new() { Id = 5, NomeCompleto = "Prof. Fernando (Turma D)", Cpf = "222.222.222-04", TipoPessoaId = 3, UserId = "prof.d", Senha = "123", Telefone = "11999990005", Ativo = true },
                new() { Id = 6, NomeCompleto = "Profª. Juliana (Turma E)", Cpf = "222.222.222-05", TipoPessoaId = 3, UserId = "prof.e", Senha = "123", Telefone = "11999990006", Ativo = true },
                // Alunos (Ids = 7..16)
                new() { Id = 7, NomeCompleto = "Joãozinho Silva", TipoPessoaId = 1, Ativo = true },
                new() { Id = 8, NomeCompleto = "Mariazinha Santos", TipoPessoaId = 1, Ativo = true },
                new() { Id = 9, NomeCompleto = "Pedrinho Oliveira", TipoPessoaId = 1, Ativo = true },
                new() { Id = 10, NomeCompleto = "Ana Costa", TipoPessoaId = 1, Ativo = true },
                new() { Id = 11, NomeCompleto = "Lucas Pereira", TipoPessoaId = 1, Ativo = true },
                new() { Id = 12, NomeCompleto = "Julia Rodrigues", TipoPessoaId = 1, Ativo = true },
                new() { Id = 13, NomeCompleto = "Marcos Almeida", TipoPessoaId = 1, Ativo = true },
                new() { Id = 14, NomeCompleto = "Sofia Nascimento", TipoPessoaId = 1, Ativo = true },
                new() { Id = 15, NomeCompleto = "Gabriel Lima", TipoPessoaId = 1, Ativo = true },
                new() { Id = 16, NomeCompleto = "Beatriz Araújo", TipoPessoaId = 1, Ativo = true },
                // Exemplo de Responsável (Id = 17)
                new() { Id = 17, NomeCompleto = "Roberto Silva (Pai do Joãozinho)", Cpf = "333.333.333-33", TipoPessoaId = 2, Telefone = "11988887777", Ativo = true }
            };
            await context.Pessoas.AddRangeAsync(pessoas);
            await context.SaveChangesAsync();
        }

        // 6. Turmas (5 turmas conforme cenário da escola)
        if (!await context.Turmas.AnyAsync())
        {
            var turmas = new List<Turma>
            {
                new() { Id = 1, Nome = "Turma A", AnoLetivo = 2026, TurnoId = 1, ProfessorPessoaId = 2, Sala = "Sala 101", Ativo = true },
                new() { Id = 2, Nome = "Turma B", AnoLetivo = 2026, TurnoId = 1, ProfessorPessoaId = 3, Sala = "Sala 102", Ativo = true },
                new() { Id = 3, Nome = "Turma C", AnoLetivo = 2026, TurnoId = 1, ProfessorPessoaId = 4, Sala = "Sala 103", Ativo = true },
                new() { Id = 4, Nome = "Turma D", AnoLetivo = 2026, TurnoId = 1, ProfessorPessoaId = 5, Sala = "Sala 104", Ativo = true },
                new() { Id = 5, Nome = "Turma E", AnoLetivo = 2026, TurnoId = 1, ProfessorPessoaId = 6, Sala = "Sala 105", Ativo = true }
            };
            await context.Turmas.AddRangeAsync(turmas);
            await context.SaveChangesAsync();
        }

        // 7. Alunos
        if (!await context.Alunos.AnyAsync())
        {
            var alunos = new List<Aluno>
            {
                new() { Id = 1, PessoaId = 7, Matricula = "MAT2026001", TurmaId = 1, PodeSairSozinho = false, Ativo = true },
                new() { Id = 2, PessoaId = 8, Matricula = "MAT2026002", TurmaId = 1, PodeSairSozinho = false, Ativo = true },
                new() { Id = 3, PessoaId = 9, Matricula = "MAT2026003", TurmaId = 2, PodeSairSozinho = false, Ativo = true },
                new() { Id = 4, PessoaId = 10, Matricula = "MAT2026004", TurmaId = 2, PodeSairSozinho = false, Ativo = true },
                new() { Id = 5, PessoaId = 11, Matricula = "MAT2026005", TurmaId = 3, PodeSairSozinho = false, Ativo = true },
                new() { Id = 6, PessoaId = 12, Matricula = "MAT2026006", TurmaId = 3, PodeSairSozinho = false, Ativo = true },
                new() { Id = 7, PessoaId = 13, Matricula = "MAT2026007", TurmaId = 4, PodeSairSozinho = false, Ativo = true },
                new() { Id = 8, PessoaId = 14, Matricula = "MAT2026008", TurmaId = 4, PodeSairSozinho = false, Ativo = true },
                new() { Id = 9, PessoaId = 15, Matricula = "MAT2026009", TurmaId = 5, PodeSairSozinho = false, Ativo = true },
                new() { Id = 10, PessoaId = 16, Matricula = "MAT2026010", TurmaId = 5, PodeSairSozinho = false, Ativo = true }
            };
            await context.Alunos.AddRangeAsync(alunos);
            await context.SaveChangesAsync();
        }

        // 8. AlunoResponsavel
        if (!await context.AlunosResponsaveis.AnyAsync())
        {
            await context.AlunosResponsaveis.AddAsync(new AlunoResponsavel
            {
                Id = 1,
                AlunoId = 1,
                ResponsavelPessoaId = 17,
                TipoVinculoId = 2, // Pai
                ResponsavelPrincipal = true,
                PodeRetirar = true,
                Ativo = true
            });
            await context.SaveChangesAsync();
        }

        // 9. Sincroniza as sequences (auto-incremento) do PostgreSQL com o maior ID de cada tabela
        var tabelas = new[]
        {
            "tipo_pessoa",
            "turno",
            "tipo_vinculo",
            "status_saida",
            "pessoa",
            "turma",
            "aluno",
            "aluno_responsavel",
            "solicitacao_saida",
            "historico_saida"
        };

        foreach (var tabela in tabelas)
        {
            var sql = $"SELECT setval(pg_get_serial_sequence('{tabela}', 'id'), COALESCE((SELECT MAX(id) FROM {tabela}), 1), true);";
            await context.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
