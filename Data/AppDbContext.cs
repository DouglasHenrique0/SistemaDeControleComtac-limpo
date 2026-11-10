using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Data;

public class AppDbContext : IdentityDbContext<UsuarioIdentity>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<RegistroVeiculo> RegistrosVeiculos =>
        Set<RegistroVeiculo>();

    public DbSet<Receita> Receitas =>
        Set<Receita>();

    public DbSet<Despesa> Despesas =>
        Set<Despesa>();

    public DbSet<RelatorioExcelGerado> RelatoriosExcelGerados =>
        Set<RelatorioExcelGerado>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Receita>()
            .HasOne(receita => receita.RegistroVeiculo)
            .WithMany(registro => registro.Receitas)
            .HasForeignKey(receita => receita.RegistroVeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Receita>()
            .Property(receita => receita.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Entity<Receita>()
            .Property(receita => receita.CategoriaTarifa)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Entity<Receita>()
            .Property(receita => receita.Pagamento)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Entity<Receita>()
            .Property(receita => receita.StatusFinanceiro)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Entity<Despesa>()
            .Property(despesa => despesa.Categoria)
            .HasConversion<string>()
            .HasMaxLength(60);

        builder.Entity<Despesa>()
            .Property(despesa => despesa.Pagamento)
            .HasConversion<string>()
            .HasMaxLength(30);
    }
}
