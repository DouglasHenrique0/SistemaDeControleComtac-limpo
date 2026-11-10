namespace SistemaDeControleComtac.Models;

public enum CategoriaDespesa
{
    ContasBasicas = 1,
    FolhaPagamentoFuncionarios = 2,
    ImpostosTaxas = 3,
    FornecedoresInsumos = 4,
    ManutencaoEquipamentos = 5,
    DespesasAdministrativasDiversas = 6,
    Outros = 7
}

public static class CategoriaDespesaExtensions
{
    public static IReadOnlyList<string> Opcoes { get; } =
    [
        "Contas básicas",
        "Folha de pagamento / funcionários",
        "Impostos e taxas",
        "Fornecedores e insumos",
        "Manutenção de equipamentos",
        "Despesas administrativas diversas",
        "Outros"
    ];

    public static string ObterDescricao(this CategoriaDespesa categoria) => categoria switch
    {
        CategoriaDespesa.ContasBasicas => "Contas básicas",
        CategoriaDespesa.FolhaPagamentoFuncionarios => "Folha de pagamento / funcionários",
        CategoriaDespesa.ImpostosTaxas => "Impostos e taxas",
        CategoriaDespesa.FornecedoresInsumos => "Fornecedores e insumos",
        CategoriaDespesa.ManutencaoEquipamentos => "Manutenção de equipamentos",
        CategoriaDespesa.DespesasAdministrativasDiversas => "Despesas administrativas diversas",
        CategoriaDespesa.Outros => "Outros",
        _ => categoria.ToString()
    };
}
