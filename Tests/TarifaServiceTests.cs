using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Tests;

public class TarifaServiceTests
{
    public static TheoryData<CategoriaTarifa, bool, decimal> Tarifas => new()
    {
        { CategoriaTarifa.Escolar, true, 301.47m },
        { CategoriaTarifa.Escolar, false, 211.38m },
        { CategoriaTarifa.Pesado, true, 484.08m },
        { CategoriaTarifa.Pesado, false, 393.99m }
    };

    [Theory]
    [MemberData(nameof(Tarifas))]
    public void Calcular_RetornaValorCorreto(CategoriaTarifa categoria, bool incluiGru, decimal esperado)
    {
        var valor = TarifaService.Calcular(categoria, incluiGru);

        Assert.Equal(esperado, valor);
    }

    [Fact]
    public void Calcular_RejeitaCategoriaInvalida()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TarifaService.Calcular((CategoriaTarifa)999, incluiGru: true));
    }
}
