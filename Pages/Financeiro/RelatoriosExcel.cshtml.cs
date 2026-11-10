using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Data;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Pages.Financeiro;

[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class RelatoriosExcelModel : PageModel
{
    private const string TipoConteudo = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");
    private readonly AppDbContext _context;
    private readonly RelatorioFinanceiroService _relatorios;
    private readonly RelatorioExcelService _excel;
    private readonly RelatorioArquivoStorageService _storage;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RelatoriosExcelModel> _logger;

    public RelatoriosExcelModel(
        AppDbContext context,
        RelatorioFinanceiroService relatorios,
        RelatorioExcelService excel,
        RelatorioArquivoStorageService storage,
        TimeProvider timeProvider,
        ILogger<RelatoriosExcelModel> logger)
    {
        _context = context;
        _relatorios = relatorios;
        _excel = excel;
        _storage = storage;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int? Ano { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Mes { get; set; }

    public IReadOnlyList<int> AnosDisponiveis { get; private set; } = [];
    public IReadOnlyList<GrupoRelatoriosAno> Grupos { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        NormalizarCompetencia();
        await CarregarPaginaAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostGerarAsync(CancellationToken cancellationToken)
    {
        NormalizarCompetencia();
        if (!ModelState.IsValid)
        {
            await CarregarPaginaAsync(cancellationToken);
            return Page();
        }

        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Forbid();
        }

        ArquivoRelatorioSalvo? arquivoSalvo = null;

        try
        {
            var agora = _timeProvider.GetLocalNow().DateTime;
            var relatorio = await _relatorios.ObterAsync(Ano!.Value, Mes!.Value, cancellationToken);
            var conteudo = _excel.Criar(relatorio, agora);
            arquivoSalvo = await _storage.SalvarAsync(Ano.Value, Mes.Value, conteudo, cancellationToken);

            _context.RelatoriosExcelGerados.Add(new RelatorioExcelGerado
            {
                Ano = Ano.Value,
                Mes = Mes.Value,
                NomeArquivo = arquivoSalvo.NomeArquivo,
                CaminhoRelativo = arquivoSalvo.CaminhoRelativo,
                DataGeracao = agora,
                UsuarioGeradorId = usuarioId
            });
            await _context.SaveChangesAsync(cancellationToken);

            TempData[NotificacaoChaves.Sucesso] = $"{arquivoSalvo.NomeArquivo} foi gerado e guardado com segurança.";
            return RedirectToPage(new { ano = Ano, mes = Mes });
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or DbUpdateException)
        {
            if (arquivoSalvo is not null)
            {
                _storage.ExcluirSeExistir(arquivoSalvo.CaminhoRelativo);
            }

            _logger.LogError(ex, "Falha ao gerar o relatório Excel de {Mes}/{Ano}.", Mes, Ano);
            ModelState.AddModelError(string.Empty, "Não foi possível gerar o relatório. Tente novamente; nenhum arquivo anterior foi alterado.");
            await CarregarPaginaAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnGetBaixarAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var relatorio = await _context.RelatoriosExcelGerados
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (relatorio is null)
        {
            return NotFound();
        }

        try
        {
            var conteudo = await _storage.LerAsync(relatorio.CaminhoRelativo, cancellationToken);
            return conteudo is null
                ? NotFound()
                : File(conteudo, TipoConteudo, relatorio.NomeArquivo);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Caminho inválido no relatório Excel {RelatorioId}.", id);
            return NotFound();
        }
    }

    private void NormalizarCompetencia()
    {
        var agora = _timeProvider.GetLocalNow();
        Ano ??= agora.Year;
        Mes ??= agora.Month;

        if (Ano is < 2000 or > 2100)
        {
            ModelState.AddModelError(nameof(Ano), "Escolha um ano válido.");
            Ano = agora.Year;
        }

        if (Mes is < 1 or > 12)
        {
            ModelState.AddModelError(nameof(Mes), "Escolha um mês válido.");
            Mes = agora.Month;
        }
    }

    private async Task CarregarPaginaAsync(CancellationToken cancellationToken)
    {
        var agora = _timeProvider.GetLocalNow();
        var metadados = await _context.RelatoriosExcelGerados
            .AsNoTracking()
            .OrderByDescending(item => item.Ano)
            .ThenByDescending(item => item.Mes)
            .ThenByDescending(item => item.DataGeracao)
            .ToListAsync(cancellationToken);

        var anosFinanceiros = await _relatorios.ObterAnosDisponiveisAsync(agora.Year, cancellationToken);
        AnosDisponiveis = anosFinanceiros
            .Concat(metadados.Select(item => item.Ano))
            .Append(agora.Year)
            .Distinct()
            .OrderByDescending(ano => ano)
            .ToList();

        Grupos = metadados
            .GroupBy(item => item.Ano)
            .Select(grupoAno => new GrupoRelatoriosAno(
                grupoAno.Key,
                grupoAno
                    .GroupBy(item => item.Mes)
                    .Select(grupoMes => new GrupoRelatoriosMes(
                        grupoMes.Key,
                        ObterNomeMes(grupoMes.Key),
                        grupoMes.OrderByDescending(item => item.DataGeracao).ToList()))
                    .OrderByDescending(grupoMes => grupoMes.Mes)
                    .ToList()))
            .OrderByDescending(grupoAno => grupoAno.Ano)
            .ToList();
    }

    private static string ObterNomeMes(int mes)
        => CulturaPtBr.TextInfo.ToTitleCase(new DateTime(2000, mes, 1).ToString("MMMM", CulturaPtBr));
}

public sealed record GrupoRelatoriosAno(int Ano, IReadOnlyList<GrupoRelatoriosMes> Meses);
public sealed record GrupoRelatoriosMes(int Mes, string Nome, IReadOnlyList<RelatorioExcelGerado> Arquivos);
