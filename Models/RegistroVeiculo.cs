using System.ComponentModel.DataAnnotations;

namespace SistemaDeControleComtac.Models;

[Microsoft.EntityFrameworkCore.Index(nameof(DataHoraEntrada))]
public class RegistroVeiculo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "A placa do veículo é obrigatória.")]
    [StringLength(7, MinimumLength = 7, ErrorMessage = "A placa do veículo deve ter exatamente 7 caracteres.")]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "A empresa é obrigatória.")]
    [StringLength(200, ErrorMessage = "A empresa deve ter no máximo 200 caracteres.")]
    public string Empresa { get; set; } = string.Empty;

    public int NumeroDoDia { get; set; }

    [Required(ErrorMessage = "O número da OS é obrigatório.")]
    [StringLength(50, ErrorMessage = "A OS deve ter no máximo 50 caracteres.")]
    public string OSComtac { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "A frota deve ter no máximo 50 caracteres.")]
    public string? Frota { get; set; }

    [Required(ErrorMessage = "Informe o cliente.")]
    [StringLength(200, ErrorMessage = "O cliente deve ter no máximo 200 caracteres.")]
    public string Cliente { get; set; } = string.Empty;

    [Required(ErrorMessage = "Qual o modelo do TCO?.")]
    [StringLength(100, ErrorMessage = "O modelo do TCO deve ter no máximo 100 caracteres.")]
    public string ModeloTCO { get; set; } = string.Empty;

    [Required(ErrorMessage = "informe o número de série do TCO.")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "O número de série do TCO deve ter exatamente 8 caracteres.")]
    public string NumeroSerie { get; set; } = string.Empty;

    public DateTime DataHoraEntrada { get; set; } = DateTime.Now;

    [StringLength(200, ErrorMessage = "Os serviços devem ter no máximo 200 caracteres.")]
    public string? ServicosRealizados { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "As atividades devem ter no máximo 2000 caracteres.")]
    public string? AtividadesRealizadas { get; set; }

    [StringLength(100, ErrorMessage = "O técnico deve ter no máximo 100 caracteres.")]
    public string? TecnicoResponsavel { get; set; } = string.Empty;

    public DateTime? DataHoraSaida { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = RegistroStatus.NoPatio;

    public bool FezConserto { get; set; }

    public bool FezSelagem { get; set; }

    public bool FezEnsaio { get; set; }

    [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
    public string? DescricaoConserto { get; set; } = string.Empty;

    public ICollection<Receita> Receitas { get; set; } = new List<Receita>();

    public void SincronizarServicosRealizados()
    {
        var servicos = new List<string>();

        if (FezConserto)
            servicos.Add("Conserto");

        if (FezSelagem)
            servicos.Add("Selagem / ensaio");
        else if (FezEnsaio)
            servicos.Add("Ensaio");

        ServicosRealizados = servicos.Count == 0 ? null : string.Join(" | ", servicos);
    }

    public void RestaurarFlagsDeServicos()
    {
        var texto = ServicosRealizados ?? string.Empty;
        FezConserto = texto.Contains("Conserto", StringComparison.OrdinalIgnoreCase);
        FezSelagem = texto.Contains("Selagem", StringComparison.OrdinalIgnoreCase);
        FezEnsaio = texto.Contains("Ensaio", StringComparison.OrdinalIgnoreCase);
    }
}
