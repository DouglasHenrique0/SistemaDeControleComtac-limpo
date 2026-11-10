using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Tests;

public class ReceitaFactoryTests
{
    [Fact]
    public void CriarParaSaida_ComDoisServicos_GeraDoisLancamentos()
    {
        var registro = RegistroBuilder.Criar(new DateTime(2026, 8, 23, 8, 0, 0));
        registro.Id = 42;
        var data = new DateTime(2026, 8, 23, 17, 30, 0);

        var receitas = ReceitaFactory.CriarParaSaida(
            registro,
            fezSelagem: true,
            CategoriaTarifa.Pesado,
            incluiGru: false,
            fezConserto: true,
            descricaoConserto: "Troca do lacre",
            valorConserto: 127.456m,
            data);

        Assert.Equal(2, receitas.Count);
        var selagem = Assert.Single(receitas, receita => receita.Tipo == TipoReceita.Selagem);
        var conserto = Assert.Single(receitas, receita => receita.Tipo == TipoReceita.Conserto);
        Assert.Equal(393.99m, selagem.Valor);
        Assert.Equal(CategoriaTarifa.Pesado, selagem.CategoriaTarifa);
        Assert.False(selagem.IncluiGru);
        Assert.Equal(127.46m, conserto.Valor);
        Assert.Equal("Troca do lacre", conserto.Descricao);
        Assert.Equal(data, conserto.Data);
    }

    [Fact]
    public void CriarParaSaida_SelagemSemCategoria_RejeitaDados()
    {
        var registro = RegistroBuilder.Criar(DateTime.Today);

        Assert.Throws<ArgumentException>(() => ReceitaFactory.CriarParaSaida(
            registro, true, null, true, false, null, null, DateTime.Now));
    }

    [Fact]
    public void CriarParaSaida_ConsertoSemValor_RejeitaDados()
    {
        var registro = RegistroBuilder.Criar(DateTime.Today);

        Assert.Throws<ArgumentException>(() => ReceitaFactory.CriarParaSaida(
            registro, false, null, null, true, "Ajuste", null, DateTime.Now));
    }
}
