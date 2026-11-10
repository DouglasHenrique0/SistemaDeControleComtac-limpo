using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Pages.Veiculos;

namespace SistemaDeControleComtac.Tests;

public class RegistrarEntradaModelTests
{
    [Fact]
    public async Task OnPostAsync_RegistraEntradaNormalizadaComProximoNumero()
    {
        await using var database = await TestDatabase.CreateAsync();
        var agora = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        var primeiro = RegistroBuilder.Criar(agora.Date, "AAA1A11");
        primeiro.NumeroDoDia = 1;
        var segundo = RegistroBuilder.Criar(agora.Date.AddHours(1), "BBB2B22");
        segundo.NumeroDoDia = 2;
        database.Context.AddRange(primeiro, segundo);
        await database.Context.SaveChangesAsync();

        var page = new RegistrarEntradaModel(database.Context, new FixedTimeProvider(agora))
        {
            Input = new RegistrarEntradaModel.EntradaInput
            {
                Placa = "abc1d23",
                Frota = "  F-10  ",
                Empresa = "  Empresa Teste  ",
                OSComtac = "  OS-123  ",
                Cliente = "  Cliente Teste  ",
                ModeloTCO = "  Modelo X  ",
                NumeroSerie = "ab12cd34"
            }
        }.ComTempData();

        var result = await page.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Veiculos/RegistrarEntrada", redirect.PageName);
        var criado = await database.Context.RegistrosVeiculos.SingleAsync(registro => registro.Placa == "ABC1D23");
        Assert.Equal(3, criado.NumeroDoDia);
        Assert.Equal(agora.DateTime, criado.DataHoraEntrada);
        Assert.Equal("No pátio", criado.Status);
        Assert.Equal("F-10", criado.Frota);
        Assert.Equal("Empresa Teste", criado.Empresa);
        Assert.Equal("OS-123", criado.OSComtac);
        Assert.Equal("Cliente Teste", criado.Cliente);
        Assert.Equal("Modelo X", criado.ModeloTCO);
        Assert.Equal("AB12CD34", criado.NumeroSerie);
        Assert.Null(criado.DataHoraSaida);
        Assert.False(criado.FezConserto);
        Assert.False(criado.FezSelagem);
    }

    [Fact]
    public async Task OnPostAsync_RecusaSegundaEntradaAtivaParaMesmaPlaca()
    {
        await using var database = await TestDatabase.CreateAsync();
        var agora = new DateTimeOffset(2026, 8, 23, 12, 0, 0, TimeSpan.Zero);
        database.Context.Add(RegistroBuilder.Criar(agora.Date, "ABC1D23"));
        await database.Context.SaveChangesAsync();

        var page = new RegistrarEntradaModel(database.Context, new FixedTimeProvider(agora))
        {
            Input = new RegistrarEntradaModel.EntradaInput
            {
                Placa = "abc1d23",
                Empresa = "Empresa Teste",
                OSComtac = "OS-456",
                Cliente = "Cliente Teste",
                ModeloTCO = "Modelo X",
                NumeroSerie = "ef56gh78"
            }
        }.ComTempData();

        var result = await page.OnPostAsync();

        Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
        Assert.Contains(
            page.ModelState["Input.Placa"]!.Errors,
            error => error.ErrorMessage == "Este veículo já possui uma entrada ativa.");
        Assert.Equal(1, await database.Context.RegistrosVeiculos.CountAsync());
    }
}
