using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SistemaDeControleComtac.Models;

[Index(nameof(RegistroVeiculoId), nameof(Tipo), IsUnique = true)]
[Index(nameof(Data))]
public class Receita
{
    public int Id { get; set; }

    [Required]
    public int RegistroVeiculoId { get; set; }

    public RegistroVeiculo RegistroVeiculo { get; set; } = null!;

    public TipoReceita Tipo { get; set; } = TipoReceita.Outro;

    public CategoriaTarifa? CategoriaTarifa { get; set; }

    public bool? IncluiGru { get; set; }

    [StringLength(200)]
    public string? Destinatario { get; set; }

    [StringLength(100)]
    public string? Documento { get; set; }

    public FormaPagamento? Pagamento { get; set; }

    public StatusFinanceiro StatusFinanceiro { get; set; } = StatusFinanceiro.Pendente;

    public DateTime? DataRecebimento { get; set; }

    [StringLength(500)]
    public string? Nota { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Informe um valor válido.")]
    [Precision(18, 2)]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(200, ErrorMessage = "A descrição deve ter no máximo 200 caracteres.")]
    public string Descricao { get; set; } = string.Empty;

    public DateTime Data { get; set; } = DateTime.Now;
}
