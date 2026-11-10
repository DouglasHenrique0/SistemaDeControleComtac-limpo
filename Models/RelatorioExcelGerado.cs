using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SistemaDeControleComtac.Models;

[Index(nameof(Ano), nameof(Mes), nameof(DataGeracao))]
[Index(nameof(CaminhoRelativo), IsUnique = true)]
public class RelatorioExcelGerado
{
    public int Id { get; set; }

    [Range(1, 12)]
    public int Mes { get; set; }

    [Range(2000, 2100)]
    public int Ano { get; set; }

    [Required]
    [StringLength(180)]
    public string NomeArquivo { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string CaminhoRelativo { get; set; } = string.Empty;

    public DateTime DataGeracao { get; set; }

    [Required]
    [StringLength(450)]
    public string UsuarioGeradorId { get; set; } = string.Empty;
}
