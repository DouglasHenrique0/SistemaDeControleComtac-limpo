using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SistemaDeControleComtac.Models;

[Index(nameof(Data))]
public class Despesa
{
    public int Id { get; set; }

    public DateTime Data { get; set; }

    [Required]
    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;

    public CategoriaDespesa Categoria { get; set; } = CategoriaDespesa.Outros;

    public FormaPagamento? Pagamento { get; set; }

    [Range(typeof(decimal), "0.01", "999999999.99", ParseLimitsInInvariantCulture = true)]
    [Precision(18, 2)]
    public decimal Valor { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }
}
