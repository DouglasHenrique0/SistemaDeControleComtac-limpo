using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Tests;

public class RegistroVeiculoTests
{
    [Fact]
    public void SincronizarServicosRealizados_EvitaDuplicarSelagemEEnsaio()
    {
        var registro = new RegistroVeiculo
        {
            FezConserto = true,
            FezSelagem = true,
            FezEnsaio = true
        };

        registro.SincronizarServicosRealizados();

        Assert.Equal("Conserto | Selagem / ensaio", registro.ServicosRealizados);
    }

    [Fact]
    public void RestaurarFlagsDeServicos_LeHistoricoAntigo()
    {
        var registro = new RegistroVeiculo
        {
            ServicosRealizados = "Conserto | Selagem | Ensaio"
        };

        registro.RestaurarFlagsDeServicos();

        Assert.True(registro.FezConserto);
        Assert.True(registro.FezSelagem);
        Assert.True(registro.FezEnsaio);
    }

    [Theory]
    [InlineData("No pátio")]
    [InlineData("No pátio.")]
    [InlineData("no PÁTIO")]
    public void EstaNoPatio_AceitaVariacoesHistoricas(string status)
    {
        Assert.True(RegistroStatus.EstaNoPatio(status));
    }
}
