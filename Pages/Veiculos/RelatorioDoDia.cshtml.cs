using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Data;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Pages.Veiculos;

[Authorize]
public class RelatorioDoDiaModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _timeProvider;

    public RelatorioDoDiaModel(AppDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public List<RegistroVeiculo> RegistrosDeHoje { get; set; } = new();
    public DateTime DataDoRelatorio { get; private set; }

    public int VeiculosNoPatio => RegistrosDeHoje.Count(r => RegistroStatus.EstaNoPatio(r.Status));

    public int VeiculosLiberados => RegistrosDeHoje.Count - VeiculosNoPatio;

    public int PercentualLiberado => RegistrosDeHoje.Count == 0
        ? 0
        : (int)Math.Round(VeiculosLiberados * 100d / RegistrosDeHoje.Count);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var hoje = _timeProvider.GetLocalNow().Date;
        var amanha = hoje.AddDays(1);
        DataDoRelatorio = hoje;

        RegistrosDeHoje = await _context.RegistrosVeiculos
            .AsNoTracking()
            .Where(r => r.DataHoraEntrada >= hoje && r.DataHoraEntrada < amanha)
            .OrderBy(r => r.NumeroDoDia)
            .ToListAsync(cancellationToken);
    }
}
