using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Tests;

public class EnumDescriptionTests
{
    [Fact]
    public void CategoriaDespesa_DescricoesEOpcoesPermanecemAlinhadas()
    {
        var esperado = new[]
        {
            "Contas básicas",
            "Folha de pagamento / funcionários",
            "Impostos e taxas",
            "Fornecedores e insumos",
            "Manutenção de equipamentos",
            "Despesas administrativas diversas",
            "Outros"
        };

        var descricoes = Enum.GetValues<CategoriaDespesa>()
            .Select(categoria => categoria.ObterDescricao())
            .ToArray();

        Assert.Equal(esperado, descricoes);
        Assert.Equal(esperado, CategoriaDespesaExtensions.Opcoes);
        Assert.Equal("999", ((CategoriaDespesa)999).ObterDescricao());
    }

    [Fact]
    public void FormaPagamento_DescricoesEOpcoesPermanecemAlinhadas()
    {
        var esperado = new[] { "Pix", "Dinheiro", "Cartão", "Boleto", "Transferência" };
        var descricoes = Enum.GetValues<FormaPagamento>()
            .Select(forma => forma.ObterDescricao())
            .ToArray();

        Assert.Equal(esperado, descricoes);
        Assert.Equal(esperado, FormaPagamentoExtensions.Opcoes);
        Assert.Equal("999", ((FormaPagamento)999).ObterDescricao());
    }

    [Fact]
    public void DemaisEnumsFinanceiros_UsamDescricoesExatasEFallbackSeguro()
    {
        Assert.Equal("Escolar", CategoriaTarifa.Escolar.ObterDescricao());
        Assert.Equal("Caminhão, van ou ônibus", CategoriaTarifa.Pesado.ObterDescricao());
        Assert.Equal("Categoria não informada", ((CategoriaTarifa)999).ObterDescricao());

        Assert.Equal("Pendente", StatusFinanceiro.Pendente.ObterDescricao());
        Assert.Equal("Recebido", StatusFinanceiro.Recebido.ObterDescricao());
        Assert.Equal("999", ((StatusFinanceiro)999).ObterDescricao());

        Assert.Equal("Outro", TipoReceita.Outro.ObterDescricao());
        Assert.Equal("Selagem / ensaio", TipoReceita.Selagem.ObterDescricao());
        Assert.Equal("Conserto", TipoReceita.Conserto.ObterDescricao());
        Assert.Equal("Outro", ((TipoReceita)999).ObterDescricao());
    }
}
