using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Data;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Pages.Relatorios;

[Authorize]
public class HistoricoModel : PageModel
{
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly AppDbContext _context;

    public HistoricoModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? DataInicial { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DataFinal { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Placa { get; set; }

    public IReadOnlyList<HistoricoMes> Meses { get; private set; } = [];
    public int TotalRegistros { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        ValidarFiltros();
        if (!ModelState.IsValid)
        {
            return;
        }

        var inicio = DataInicial?.Date;
        var fimExclusivo = DataFinal?.Date.AddDays(1);

        if (inicio.HasValue && fimExclusivo.HasValue && inicio.Value >= fimExclusivo.Value)
        {
            ModelState.AddModelError(string.Empty, "A data inicial não pode ser posterior à data final.");
            return;
        }

        var query = _context.RegistrosVeiculos.AsNoTracking();

        if (inicio.HasValue)
        {
            query = query.Where(registro => registro.DataHoraEntrada >= inicio.Value);
        }

        if (fimExclusivo.HasValue)
        {
            query = query.Where(registro => registro.DataHoraEntrada < fimExclusivo.Value);
        }

        var placaNormalizada = Placa?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(placaNormalizada))
        {
            Placa = placaNormalizada;
            query = query.Where(registro => registro.Placa.Contains(placaNormalizada));
        }

        var registros = await query
            .OrderByDescending(registro => registro.DataHoraEntrada)
            .ThenByDescending(registro => registro.NumeroDoDia)
            .Select(registro => new HistoricoItem
            {
                Id = registro.Id,
                NumeroDoDia = registro.NumeroDoDia,
                Placa = registro.Placa,
                Frota = registro.Frota,
                Empresa = registro.Empresa,
                Cliente = registro.Cliente,
                OSComtac = registro.OSComtac,
                DataHoraEntrada = registro.DataHoraEntrada,
                DataHoraSaida = registro.DataHoraSaida,
                Status = registro.Status,
                ServicosRealizados = registro.ServicosRealizados,
                FezConserto = registro.FezConserto,
                FezSelagem = registro.FezSelagem,
                FezEnsaio = registro.FezEnsaio,
                TecnicoResponsavel = registro.TecnicoResponsavel
            })
            .ToListAsync(cancellationToken);

        TotalRegistros = registros.Count;
        Meses = AgruparPorMes(registros);
    }

    private void ValidarFiltros()
    {
        var dataMinima = new DateTime(2000, 1, 1);
        var dataMaxima = new DateTime(2100, 12, 31);

        if (DataInicial.HasValue && (DataInicial.Value.Date < dataMinima || DataInicial.Value.Date > dataMaxima))
        {
            ModelState.AddModelError(nameof(DataInicial), "Escolha uma data inicial entre 01/01/2000 e 31/12/2100.");
        }

        if (DataFinal.HasValue && (DataFinal.Value.Date < dataMinima || DataFinal.Value.Date > dataMaxima))
        {
            ModelState.AddModelError(nameof(DataFinal), "Escolha uma data final entre 01/01/2000 e 31/12/2100.");
        }

        var placaNormalizada = Placa?.Trim();
        if (!string.IsNullOrWhiteSpace(placaNormalizada) &&
            (placaNormalizada.Length > 7 || placaNormalizada.Any(caractere => !char.IsAsciiLetterOrDigit(caractere))))
        {
            ModelState.AddModelError(nameof(Placa), "Digite até 7 letras ou números para buscar a placa.");
        }
    }

    public static IReadOnlyList<HistoricoMes> AgruparPorMes(IEnumerable<HistoricoItem> registros)
    {
        return registros
            .GroupBy(registro => new { registro.DataHoraEntrada.Year, registro.DataHoraEntrada.Month })
            .OrderByDescending(grupo => grupo.Key.Year)
            .ThenByDescending(grupo => grupo.Key.Month)
            .Select(grupo =>
            {
                var competencia = new DateTime(grupo.Key.Year, grupo.Key.Month, 1);
                var nomeMes = CulturaPtBr.TextInfo.ToTitleCase(competencia.ToString("MMMM", CulturaPtBr));
                return new HistoricoMes
                {
                    Competencia = competencia,
                    Titulo = $"{nomeMes} de {grupo.Key.Year}",
                    Registros = grupo
                        .OrderByDescending(registro => registro.DataHoraEntrada)
                        .ThenByDescending(registro => registro.NumeroDoDia)
                        .ToList()
                };
            })
            .ToList();
    }

    public sealed class HistoricoMes
    {
        public DateTime Competencia { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public IReadOnlyList<HistoricoItem> Registros { get; init; } = [];
    }

    public sealed class HistoricoItem
    {
        public int Id { get; init; }
        public int NumeroDoDia { get; init; }
        public string Placa { get; init; } = string.Empty;
        public string? Frota { get; init; }
        public string Empresa { get; init; } = string.Empty;
        public string Cliente { get; init; } = string.Empty;
        public string OSComtac { get; init; } = string.Empty;
        public DateTime DataHoraEntrada { get; init; }
        public DateTime? DataHoraSaida { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? ServicosRealizados { get; init; }
        public bool FezConserto { get; init; }
        public bool FezSelagem { get; init; }
        public bool FezEnsaio { get; init; }
        public string? TecnicoResponsavel { get; init; }

        public string Servicos
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ServicosRealizados))
                {
                    return ServicosRealizados;
                }

                var servicos = new List<string>();
                if (FezConserto) servicos.Add("Conserto");
                if (FezSelagem) servicos.Add("Selagem / ensaio");
                else if (FezEnsaio) servicos.Add("Ensaio");
                return servicos.Count == 0 ? "—" : string.Join(" | ", servicos);
            }
        }
    }
}
