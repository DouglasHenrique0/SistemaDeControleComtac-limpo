using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Pages.Financeiro;

[Authorize(Roles = ApplicationRoles.Admin)]
public class ReceitaModel : PageModel
{
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly RelatorioFinanceiroService _relatorios;
    private readonly TimeProvider _timeProvider;

    public ReceitaModel(
        RelatorioFinanceiroService relatorios,
        TimeProvider timeProvider)
    {
        _relatorios = relatorios;
        _timeProvider = timeProvider;
    }

    [BindProperty(SupportsGet = true)]
    public int? Ano { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Mes { get; set; }

    public RelatorioFinanceiroMensal Relatorio { get; private set; } = new(2000, 1, []);
    public IReadOnlyList<int> AnosDisponiveis { get; private set; } = [];
    public string CompetenciaDescricao { get; private set; } = string.Empty;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var agora = _timeProvider.GetLocalNow();
        NormalizarCompetencia(agora.Year, agora.Month);

        AnosDisponiveis = await _relatorios.ObterAnosDisponiveisAsync(agora.Year, cancellationToken);
        Relatorio = await _relatorios.ObterAsync(Ano!.Value, Mes!.Value, cancellationToken);
        CompetenciaDescricao = ObterCompetenciaDescricao(Ano.Value, Mes.Value);
    }

    private void NormalizarCompetencia(int anoAtual, int mesAtual)
    {
        Ano ??= anoAtual;
        Mes ??= mesAtual;

        if (Ano is < 2000 or > 2100)
        {
            ModelState.AddModelError(nameof(Ano), "Escolha um ano válido.");
            Ano = anoAtual;
        }

        if (Mes is < 1 or > 12)
        {
            ModelState.AddModelError(nameof(Mes), "Escolha um mês válido.");
            Mes = mesAtual;
        }
    }

    public static string ObterCompetenciaDescricao(int ano, int mes)
    {
        var competencia = new DateTime(ano, mes, 1);
        var nomeMes = CulturaPtBr.TextInfo.ToTitleCase(competencia.ToString("MMMM", CulturaPtBr));
        return $"{nomeMes} de {ano}";
    }
}
