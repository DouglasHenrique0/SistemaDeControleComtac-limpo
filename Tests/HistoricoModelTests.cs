using SistemaDeControleComtac.Pages.Relatorios;

namespace SistemaDeControleComtac.Tests;

public class HistoricoModelTests
{
    [Fact]
    public void AgruparPorMes_OrdenaMesesERegistrosDoMaisRecente()
    {
        var registros = new[]
        {
            CriarItem(new DateTime(2026, 7, 10, 8, 0, 0), 1, "JUL1A11"),
            CriarItem(new DateTime(2026, 8, 5, 9, 0, 0), 2, "AGO2B22"),
            CriarItem(new DateTime(2026, 8, 20, 10, 0, 0), 3, "AGO3C33")
        };

        var meses = HistoricoModel.AgruparPorMes(registros);

        Assert.Equal(2, meses.Count);
        Assert.Equal("Agosto de 2026", meses[0].Titulo);
        Assert.Equal(2, meses[0].Registros.Count);
        Assert.Equal("AGO3C33", meses[0].Registros[0].Placa);
        Assert.Equal("Julho de 2026", meses[1].Titulo);
    }

    [Fact]
    public async Task OnGetAsync_AplicaFiltroDePlacaEMantemMeses()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.RegistrosVeiculos.AddRange(
            RegistroBuilder.Criar(new DateTime(2026, 8, 1), "ABC1D23"),
            RegistroBuilder.Criar(new DateTime(2026, 7, 1), "XYZ9Z99"));
        await database.Context.SaveChangesAsync();

        var page = new HistoricoModel(database.Context) { Placa = "abc1d23" };
        await page.OnGetAsync();

        Assert.Equal(1, page.TotalRegistros);
        Assert.Single(page.Meses);
        Assert.Equal("ABC1D23", Assert.Single(page.Meses[0].Registros).Placa);
        Assert.Equal("ABC1D23", page.Placa);
    }

    [Fact]
    public async Task OnGetAsync_RejeitaPeriodoInvertido()
    {
        await using var database = await TestDatabase.CreateAsync();
        var page = new HistoricoModel(database.Context)
        {
            DataInicial = new DateTime(2026, 8, 10),
            DataFinal = new DateTime(2026, 8, 9)
        };

        await page.OnGetAsync();

        Assert.False(page.ModelState.IsValid);
        Assert.Empty(page.Meses);
    }

    [Fact]
    public async Task OnGetAsync_RejeitaDataExtremaSemLancarExcecao()
    {
        await using var database = await TestDatabase.CreateAsync();
        var page = new HistoricoModel(database.Context)
        {
            DataFinal = DateTime.MaxValue
        };

        await page.OnGetAsync();

        Assert.False(page.ModelState.IsValid);
        Assert.Empty(page.Meses);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("ABC_123")]
    [InlineData("PLACA-MUITO-LONGA")]
    public async Task OnGetAsync_RejeitaFiltroDePlacaComCuringaOuTamanhoInvalido(string placa)
    {
        await using var database = await TestDatabase.CreateAsync();
        var page = new HistoricoModel(database.Context) { Placa = placa };

        await page.OnGetAsync();

        Assert.False(page.ModelState.IsValid);
        Assert.Empty(page.Meses);
    }

    private static HistoricoModel.HistoricoItem CriarItem(DateTime entrada, int numero, string placa) => new()
    {
        DataHoraEntrada = entrada,
        NumeroDoDia = numero,
        Placa = placa,
        Empresa = "Empresa",
        Cliente = "Cliente",
        OSComtac = "OS",
        Status = "Liberado"
    };
}
