using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Pages.Veiculos.RegistrarSaida;

namespace SistemaDeControleComtac.Tests;

public class RegistrarSaidaModelTests
{
    [Fact]
    public async Task OnPostAsync_LiberaVeiculoECriaReceitasDosServicos()
    {
        await using var database = await TestDatabase.CreateAsync();
        var registro = RegistroBuilder.Criar(new DateTime(2026, 8, 23, 8, 0, 0));
        database.Context.Add(registro);
        await database.Context.SaveChangesAsync();
        var agora = new DateTimeOffset(2026, 8, 23, 17, 0, 0, TimeSpan.Zero);

        var page = new RegistrarSaidaModel(database.Context, new FixedTimeProvider(agora))
        {
            Input = new RegistrarSaidaModel.SaidaInput
            {
                RegistroId = registro.Id,
                FezSelagem = true,
                CategoriaTarifa = CategoriaTarifa.Escolar,
                IncluiGru = true,
                FezConserto = true,
                DescricaoConserto = "Ajuste no equipamento",
                ValorConserto = 80m,
                TecnicoResponsavel = "Ana",
                ConfirmarSaida = true
            }
        }.ComTempData();

        var result = await page.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Veiculos/RelatorioDoDia", redirect.PageName);
        var atualizado = await database.Context.RegistrosVeiculos.AsNoTracking().SingleAsync();
        Assert.Equal(RegistroStatus.Liberado, atualizado.Status);
        Assert.Equal(agora.DateTime, atualizado.DataHoraSaida);
        Assert.True(atualizado.FezSelagem);
        Assert.True(atualizado.FezEnsaio);
        Assert.Equal("Ana", atualizado.TecnicoResponsavel);

        var receitas = await database.Context.Receitas.AsNoTracking().OrderBy(item => item.Tipo).ToListAsync();
        Assert.Equal(2, receitas.Count);
        Assert.Contains(receitas, receita => receita.Tipo == TipoReceita.Selagem && receita.Valor == 301.47m);
        Assert.Contains(receitas, receita => receita.Tipo == TipoReceita.Conserto && receita.Valor == 80m);
    }

    [Fact]
    public async Task OnPostAsync_ConsertoSemValor_NaoLiberaVeiculo()
    {
        await using var database = await TestDatabase.CreateAsync();
        var registro = RegistroBuilder.Criar(new DateTime(2026, 8, 23, 8, 0, 0));
        database.Context.Add(registro);
        await database.Context.SaveChangesAsync();

        var page = new RegistrarSaidaModel(
            database.Context,
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 23, 17, 0, 0, TimeSpan.Zero)))
        {
            Input = new RegistrarSaidaModel.SaidaInput
            {
                RegistroId = registro.Id,
                FezConserto = true,
                DescricaoConserto = "Ajuste",
                ConfirmarSaida = true
            }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        var atualizado = await database.Context.RegistrosVeiculos.AsNoTracking().SingleAsync();
        Assert.Equal(RegistroStatus.NoPatio, atualizado.Status);
        Assert.Empty(await database.Context.Receitas.ToListAsync());
    }

    [Fact]
    public async Task OnPostAsync_SaidaDuplicada_EBloqueada()
    {
        await using var database = await TestDatabase.CreateAsync();
        var registro = RegistroBuilder.Criar(new DateTime(2026, 8, 23));
        registro.Status = RegistroStatus.Liberado;
        registro.DataHoraSaida = new DateTime(2026, 8, 23, 10, 0, 0);
        database.Context.Add(registro);
        await database.Context.SaveChangesAsync();

        var page = new RegistrarSaidaModel(database.Context, new FixedTimeProvider(DateTimeOffset.UtcNow))
        {
            Input = new RegistrarSaidaModel.SaidaInput
            {
                RegistroId = registro.Id,
                ConfirmarSaida = true
            }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.Empty(await database.Context.Receitas.ToListAsync());
    }

    [Fact]
    public async Task OnGetAsync_VeiculoJaLiberado_RedirecionaSemExibirFormulario()
    {
        await using var database = await TestDatabase.CreateAsync();
        var registro = RegistroBuilder.Criar(new DateTime(2026, 8, 23));
        registro.Status = RegistroStatus.Liberado;
        registro.DataHoraSaida = new DateTime(2026, 8, 23, 10, 0, 0);
        database.Context.Add(registro);
        await database.Context.SaveChangesAsync();

        var page = new RegistrarSaidaModel(database.Context, new FixedTimeProvider(DateTimeOffset.UtcNow))
            .ComTempData();

        var result = await page.OnGetAsync(registro.Id);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Veiculos/RelatorioDoDia", redirect.PageName);
    }
}
