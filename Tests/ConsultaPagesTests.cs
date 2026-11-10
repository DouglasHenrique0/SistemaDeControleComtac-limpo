using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaDeControleComtac.Pages.Veiculos;

namespace SistemaDeControleComtac.Tests;

public class ConsultaPagesTests
{
    [Fact]
    public async Task RelatorioDoDia_UsaDataControladaEIgnoraOutrosDias()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.RegistrosVeiculos.AddRange(
            RegistroBuilder.Criar(new DateTime(2026, 8, 23, 8, 0, 0), "ABC1D23"),
            RegistroBuilder.Criar(new DateTime(2026, 8, 22, 8, 0, 0), "XYZ9Z99"));
        await database.Context.SaveChangesAsync();

        var agora = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var page = new RelatorioDoDiaModel(database.Context, new FixedTimeProvider(agora));

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(agora.Date, page.DataDoRelatorio);
        Assert.Equal("ABC1D23", Assert.Single(page.RegistrosDeHoje).Placa);
    }

    [Fact]
    public async Task Detalhes_SemId_RedirecionaParaRelatorioDoDia()
    {
        await using var database = await TestDatabase.CreateAsync();
        var page = new DetalhesModel(database.Context);

        var result = await page.OnGetAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Veiculos/RelatorioDoDia", redirect.PageName);
    }

    [Fact]
    public async Task Detalhes_ComIdExistente_CarregaRegistroSemRastrearAlteracoes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var registro = RegistroBuilder.Criar(new DateTime(2026, 8, 23));
        database.Context.RegistrosVeiculos.Add(registro);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var page = new DetalhesModel(database.Context) { Id = registro.Id };

        var result = await page.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal(registro.Id, page.Veiculo.Id);
        Assert.Empty(database.Context.ChangeTracker.Entries());
    }
}
