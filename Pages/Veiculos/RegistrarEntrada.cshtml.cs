using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaDeControleComtac.Data;
using SistemaDeControleComtac.Models;
using SistemaDeControleComtac.Services;

namespace SistemaDeControleComtac.Pages.Veiculos;

[Authorize]
public class RegistrarEntradaModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RegistrarEntradaModel> _logger;
    private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(15);

    public RegistrarEntradaModel(
        AppDbContext context,
        TimeProvider timeProvider,
        ILogger<RegistrarEntradaModel>? logger = null)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger ?? NullLogger<RegistrarEntradaModel>.Instance;
    }

    [BindProperty]
    public EntradaInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var agora = _timeProvider.GetLocalNow().DateTime;
        var placa = Input.Placa.Trim().ToUpperInvariant();
        var entradaDuplicada = false;

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(RegistrationTimeout);

        try
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                entradaDuplicada = false;
                _context.ChangeTracker.Clear();

                await using var transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    timeoutSource.Token);

                entradaDuplicada = await _context.RegistrosVeiculos.AnyAsync(
                    registro => registro.Placa == placa && registro.DataHoraSaida == null,
                    timeoutSource.Token);

                if (entradaDuplicada)
                {
                    return;
                }

                var registro = new RegistroVeiculo
                {
                    Placa = placa,
                    Frota = NormalizarTextoOpcional(Input.Frota),
                    Empresa = Input.Empresa.Trim(),
                    OSComtac = Input.OSComtac.Trim(),
                    Cliente = Input.Cliente.Trim(),
                    ModeloTCO = Input.ModeloTCO.Trim(),
                    NumeroSerie = Input.NumeroSerie.Trim().ToUpperInvariant(),
                    DataHoraEntrada = agora,
                    Status = RegistroStatus.NoPatio,
                    NumeroDoDia = await ObterProximoNumeroDoDiaAsync(agora.Date, timeoutSource.Token),
                    FezConserto = false,
                    FezSelagem = false,
                    FezEnsaio = false,
                    ServicosRealizados = string.Empty,
                    DescricaoConserto = string.Empty,
                    TecnicoResponsavel = string.Empty
                };

                _context.RegistrosVeiculos.Add(registro);
                await _context.SaveChangesAsync(timeoutSource.Token);
                await transaction.CommitAsync(timeoutSource.Token);
            });
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Vehicle entry registration timed out. {Plate} {TimeoutSeconds}",
                placa,
                RegistrationTimeout.TotalSeconds);
            ModelState.AddModelError(string.Empty, "O banco demorou para responder. Tente novamente.");
            return Page();
        }
        catch (RetryLimitExceededException exception)
        {
            _logger.LogError(exception, "Vehicle entry registration exhausted database retries. {Plate}", placa);
            ModelState.AddModelError(string.Empty, "Não foi possível registrar o veículo agora. Tente novamente.");
            return Page();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Vehicle entry registration failed while saving. {Plate}", placa);
            ModelState.AddModelError(string.Empty, "Não foi possível salvar o veículo. Verifique os dados e tente novamente.");
            return Page();
        }

        if (entradaDuplicada)
        {
            ModelState.AddModelError("Input.Placa", "Este veículo já possui uma entrada ativa.");
            return Page();
        }

        TempData[NotificacaoChaves.Sucesso] = "Veículo registrado com sucesso.";
        return RedirectToPage("/Veiculos/RegistrarEntrada");
    }

    private async Task<int> ObterProximoNumeroDoDiaAsync(DateTime hoje, CancellationToken cancellationToken)
    {
        var fim = hoje.AddDays(1);

        var ultimoNumeroDoDia = await _context.RegistrosVeiculos
            .Where(registro => registro.DataHoraEntrada >= hoje && registro.DataHoraEntrada < fim)
            .OrderByDescending(registro => registro.NumeroDoDia)
            .Select(registro => (int?)registro.NumeroDoDia)
            .FirstOrDefaultAsync(cancellationToken);

        return (ultimoNumeroDoDia ?? 0) + 1;
    }

    private static string? NormalizarTextoOpcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    public sealed class EntradaInput
    {
        [Required(ErrorMessage = "A placa do veículo é obrigatória.")]
        [RegularExpression("^[A-Za-z]{3}[0-9][A-Za-z0-9][0-9]{2}$", ErrorMessage = "Informe uma placa válida com 7 caracteres.")]
        [Display(Name = "Placa")]
        public string Placa { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "A frota deve ter no máximo 50 caracteres.")]
        [Display(Name = "Frota")]
        public string? Frota { get; set; }

        [Required(ErrorMessage = "A empresa é obrigatória.")]
        [StringLength(150, ErrorMessage = "A empresa deve ter no máximo 150 caracteres.")]
        [Display(Name = "Empresa")]
        public string Empresa { get; set; } = string.Empty;

        [Required(ErrorMessage = "O número da OS é obrigatório.")]
        [StringLength(80, ErrorMessage = "A OS deve ter no máximo 80 caracteres.")]
        [Display(Name = "OS Comtac")]
        public string OSComtac { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o cliente.")]
        [StringLength(150, ErrorMessage = "O cliente deve ter no máximo 150 caracteres.")]
        [Display(Name = "Cliente")]
        public string Cliente { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o modelo do TCO.")]
        [StringLength(100, ErrorMessage = "O modelo do TCO deve ter no máximo 100 caracteres.")]
        [Display(Name = "Modelo TCO")]
        public string ModeloTCO { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o número de série do TCO.")]
        [RegularExpression("^[A-Za-z0-9]{8}$", ErrorMessage = "O número de série do TCO deve ter exatamente 8 letras ou números.")]
        [Display(Name = "Número de série")]
        public string NumeroSerie { get; set; } = string.Empty;
    }
}
