using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Pages.Veiculos;
using SistemaDeControleComtac.Pages.Veiculos.RegistrarSaida;

namespace SistemaDeControleComtac.Tests;

public class ValidationTests
{
    [Fact]
    public void RegistroVeiculo_PlacaIncompleta_EInvalida()
    {
        var registro = RegistroBuilder.Criar(DateTime.Today, "ABC123");

        var resultados = Validar(registro);

        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(RegistroVeiculo.Placa)));
    }

    [Fact]
    public void SaidaInput_ValorNegativo_EInvalido()
    {
        var input = new RegistrarSaidaModel.SaidaInput
        {
            RegistroId = 1,
            ValorConserto = -1m
        };

        var resultados = Validar(input);

        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(input.ValorConserto)));
    }

    [Fact]
    public void EntradaInput_PlacaOuSerieComCaracteresInvalidos_EInvalida()
    {
        var input = new RegistrarEntradaModel.EntradaInput
        {
            Placa = "ABC-123",
            Empresa = "Empresa",
            OSComtac = "OS-1",
            Cliente = "Cliente",
            ModeloTCO = "Modelo",
            NumeroSerie = "1234-678"
        };

        var resultados = Validar(input);

        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(input.Placa)));
        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(input.NumeroSerie)));
    }

    [Fact]
    public void SaidaInput_DescricaoComMaisDeDuzentosCaracteres_EInvalida()
    {
        var input = new RegistrarSaidaModel.SaidaInput
        {
            RegistroId = 1,
            DescricaoConserto = new string('A', 201)
        };

        var resultados = Validar(input);

        Assert.Contains(resultados, resultado => resultado.MemberNames.Contains(nameof(input.DescricaoConserto)));
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    public void ValidacoesMonetarias_FuncionamIndependentementeDaCultura(string nomeCultura)
    {
        var culturaAnterior = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(nomeCultura);

            var saidaValida = new RegistrarSaidaModel.SaidaInput
            {
                RegistroId = 1,
                ValorConserto = 10.50m
            };
            var receitaValida = new Receita
            {
                RegistroVeiculoId = 1,
                Valor = 10.50m,
                Descricao = "Conserto"
            };
            var despesaValida = new Despesa
            {
                Data = DateTime.Today,
                Descricao = "Material",
                Valor = 10.50m
            };

            Assert.Empty(Validar(saidaValida));
            Assert.Empty(Validar(receitaValida));
            Assert.Empty(Validar(despesaValida));
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaAnterior;
        }
    }

    private static IReadOnlyList<ValidationResult> Validar(object model)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), resultados, validateAllProperties: true);
        return resultados;
    }
}
