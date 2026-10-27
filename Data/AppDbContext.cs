using Microsoft.EntityFrameworkCore;
using PainelSaida.Api.Models;

namespace PainelSaida.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Entidades Estáticas
    public DbSet<TipoPessoa> TiposPessoa => Set<TipoPessoa>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<TipoVinculo> TiposVinculo => Set<TipoVinculo>();
    public DbSet<StatusSaida> StatusSaidas => Set<StatusSaida>();

    // Entidades Principais
    public DbSet<Pessoa> Pessoas => Set<Pessoa>();
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<AlunoResponsavel> AlunosResponsaveis => Set<AlunoResponsavel>();
    public DbSet<SolicitacaoSaida> SolicitacoesSaida => Set<SolicitacaoSaida>();
    public DbSet<HistoricoSaida> HistoricosSaida => Set<HistoricoSaida>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relacionamento 1 para 0..1 entre Pessoa e Aluno
        modelBuilder.Entity<Aluno>()
            .HasIndex(a => a.PessoaId)
            .IsUnique();

        modelBuilder.Entity<Aluno>()
            .HasIndex(a => a.Matricula)
            .IsUnique();

        // Relacionamentos de SolicitacaoSaida com Pessoa (sem cascata múltipla)
        modelBuilder.Entity<SolicitacaoSaida>()
            .HasOne(s => s.ResponsavelPessoa)
            .WithMany()
            .HasForeignKey(s => s.ResponsavelPessoaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SolicitacaoSaida>()
            .HasOne(s => s.PorteiroPessoa)
            .WithMany()
            .HasForeignKey(s => s.PorteiroPessoaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SolicitacaoSaida>()
            .HasOne(s => s.ProfessorPessoa)
            .WithMany()
            .HasForeignKey(s => s.ProfessorPessoaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Turma>()
            .HasOne(t => t.ProfessorPessoa)
            .WithMany()
            .HasForeignKey(t => t.ProfessorPessoaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AlunoResponsavel>()
            .HasOne(ar => ar.ResponsavelPessoa)
            .WithMany()
            .HasForeignKey(ar => ar.ResponsavelPessoaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
