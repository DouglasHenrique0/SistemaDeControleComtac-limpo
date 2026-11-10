using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Tests;

public class RelatorioRegrasTests
{
    [Theory]
    [InlineData(true, false, false, "Conserto")]
    [InlineData(false, true, false, "Selagem com Lacração")]
    [InlineData(false, false, true, "Ensaio")]
    [InlineData(true, true, false, "Conserto + Selagem com Lacração")]
    [InlineData(true, false, true, "Conserto + Ensaio")]
    [InlineData(true, true, true, "Conserto + Selagem com Lacração")]
    public void ServicoDescricao_GeraCombinacoesEmUmaLinha(
        bool conserto,
        bool selagem,
        bool ensaio,
        string esperado)
    {
        Assert.Equal(esperado, ServicoDescricaoService.Obter(conserto, selagem, ensaio, null));
    }

    [Fact]
    public void DataOperacional_PrefereSaidaEUsaEntradaComoFallback()
    {
        var entrada = new DateTime(2026, 8, 1, 8, 0, 0);
        var saida = new DateTime(2026, 8, 2, 17, 0, 0);

        Assert.Equal(saida, DataOperacionalService.Obter(entrada, saida));
        Assert.Equal(entrada, DataOperacionalService.Obter(entrada, null));
    }
}
