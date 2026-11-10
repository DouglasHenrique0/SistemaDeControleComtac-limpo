namespace SistemaDeControleComtac.Models;

public enum StatusFinanceiro
{
    Pendente = 1,
    Recebido = 2
}

public static class StatusFinanceiroExtensions
{
    public static IReadOnlyList<string> Opcoes { get; } = ["Pendente", "Recebido"];

    public static string ObterDescricao(this StatusFinanceiro status) => status switch
    {
        StatusFinanceiro.Pendente => "Pendente",
        StatusFinanceiro.Recebido => "Recebido",
        _ => status.ToString()
    };
}
