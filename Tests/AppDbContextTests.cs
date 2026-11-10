using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Tests;

public class AppDbContextTests
{
    [Fact]
    public async Task Receitas_ImpedemDuplicarMesmoServicoNoMesmoVeiculo()
    {
        await using var database = await TestDatabase.CreateAsync();
        var registro = RegistroBuilder.Criar(DateTime.Today);
        database.Context.Add(registro);
        await database.Context.SaveChangesAsync();
        database.Context.Receitas.AddRange(
            new Receita { RegistroVeiculoId = registro.Id, Tipo = TipoReceita.Selagem, Valor = 1m, Descricao = "Um", Data = DateTime.Today },
            new Receita { RegistroVeiculoId = registro.Id, Tipo = TipoReceita.Selagem, Valor = 2m, Descricao = "Dois", Data = DateTime.Today });

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Receitas_PermitemSelagemEConsertoNoMesmoVeiculo()
    {
        await using var database = await TestDatabase.CreateAsync();
        var registro = RegistroBuilder.Criar(DateTime.Today);
        database.Context.Add(registro);
        await database.Context.SaveChangesAsync();
        database.Context.Receitas.AddRange(
            new Receita { RegistroVeiculoId = registro.Id, Tipo = TipoReceita.Selagem, Valor = 1m, Descricao = "Selagem", Data = DateTime.Today },
            new Receita { RegistroVeiculoId = registro.Id, Tipo = TipoReceita.Conserto, Valor = 2m, Descricao = "Conserto", Data = DateTime.Today });

        await database.Context.SaveChangesAsync();

        Assert.Equal(2, await database.Context.Receitas.CountAsync());
    }

    [Fact]
    public async Task Despesas_EHistoricoExcel_SaoPersistidosSeparadamente()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.Despesas.Add(new Despesa
        {
            Data = new DateTime(2026, 8, 10),
            Descricao = "Energia",
            Categoria = CategoriaDespesa.ContasBasicas,
            Pagamento = FormaPagamento.Boleto,
            Valor = 80m
        });
        database.Context.RelatoriosExcelGerados.Add(new RelatorioExcelGerado
        {
            Ano = 2026,
            Mes = 8,
            NomeArquivo = "Relatorio_Comtac_Agosto_2026.xlsx",
            CaminhoRelativo = "2026/08/Relatorio_Comtac_Agosto_2026.xlsx",
            DataGeracao = new DateTime(2026, 8, 31, 18, 0, 0),
            UsuarioGeradorId = "admin-id"
        });

        await database.Context.SaveChangesAsync();

        Assert.Single(await database.Context.Despesas.AsNoTracking().ToListAsync());
        Assert.Single(await database.Context.RelatoriosExcelGerados.AsNoTracking().ToListAsync());
    }
}
