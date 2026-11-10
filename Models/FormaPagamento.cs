using System.ComponentModel.DataAnnotations;

namespace SistemaDeControleComtac.Models;

public enum FormaPagamento
{
    Pix = 1,
    Dinheiro = 2,

    [Display(Name = "Cartão")]
    Cartao = 3,

    Boleto = 4,

    [Display(Name = "Transferência")]
    Transferencia = 5
}

public static class FormaPagamentoExtensions
{
    public static IReadOnlyList<string> Opcoes { get; } =
    [
        "Pix",
        "Dinheiro",
        "Cartão",
        "Boleto",
        "Transferência"
    ];

    public static string ObterDescricao(this FormaPagamento formaPagamento) => formaPagamento switch
    {
        FormaPagamento.Pix => "Pix",
        FormaPagamento.Dinheiro => "Dinheiro",
        FormaPagamento.Cartao => "Cartão",
        FormaPagamento.Boleto => "Boleto",
        FormaPagamento.Transferencia => "Transferência",
        _ => formaPagamento.ToString()
    };
}
