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

namespace SistemaDeControleComtac.Pages.Veiculos.RegistrarSaida;

[Authorize]
public class RegistrarSaidaModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RegistrarSaidaModel> _logger;
    private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(15);

    public RegistrarSaidaModel(
        AppDbContext context,
        TimeProvider timeProvider,
        ILogger<RegistrarSaidaModel>? logger = null)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger ?? NullLogger<RegistrarSaidaModel>.Instance;
    }

    public RegistroVeiculo RegistroVeiculo { get; private set; } = new();

    [BindProperty]
    public SaidaInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (!id.HasValue || id.Value <= 0)
        {
            return RedirectToPage("/Veiculos/RelatorioDoDia");
        }

        if (!await CarregarRegistroAsync(id.Value, inicializarInput: true))
        {
            return NotFound();
        }

        if (!RegistroStatus.EstaNoPatio(RegistroVeiculo.Status))
        {
            TempData[NotificacaoChaves.Erro] = "Este veículo já foi liberado e não pode receber uma segunda saída.";
            return RedirectToPage("/Veiculos/RelatorioDoDia");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
    {
        ValidarServicos();

        if (!ModelState.IsValid)
        {
            await CarregarRegistroAsync(Input.RegistroId, cancellationToken: cancellationToken);
            return Page();
        }

        var registroNaoEncontrado = false;
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(RegistrationTimeout);

        try
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                registroNaoEncontrado = false;
                _context.ChangeTracker.Clear();

                await using var transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    timeoutSource.Token);

                var registroExistente = await _context.RegistrosVeiculos
                    .FirstOrDefaultAsync(
                        registro =>
                            registro.Id == Input.RegistroId &&
                            (registro.Status == RegistroStatus.NoPatio || registro.Status == "No pátio."),
                        timeoutSource.Token);

                if (registroExistente is null)
                {
                    registroNaoEncontrado = true;
                    return;
                }

                var agora = _timeProvider.GetLocalNow().DateTime;
                registroExistente.FezConserto = Input.FezConserto;
                registroExistente.FezSelagem = Input.FezSelagem;
                registroExistente.FezEnsaio = Input.FezSelagem;
                registroExistente.DescricaoConserto = Input.FezConserto ? Input.DescricaoConserto?.Trim() : null;
                registroExistente.TecnicoResponsavel = string.IsNullOrWhiteSpace(Input.TecnicoResponsavel)
                    ? null
                    : Input.TecnicoResponsavel.Trim();
                registroExistente.DataHoraSaida = agora;
                registroExistente.Status = RegistroStatus.Liberado;
                registroExistente.SincronizarServicosRealizados();

                var receitas = ReceitaFactory.CriarParaSaida(
                    registroExistente,
                    Input.FezSelagem,
                    Input.CategoriaTarifa,
                    Input.IncluiGru,
                    Input.FezConserto,
                    Input.DescricaoConserto,
                    Input.ValorConserto,
                    agora);

                _context.Receitas.AddRange(receitas);
                await _context.SaveChangesAsync(timeoutSource.Token);
                await transaction.CommitAsync(timeoutSource.Token);
            });
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Vehicle exit registration timed out. {RegistrationId} {TimeoutSeconds}",
                Input.RegistroId,
                RegistrationTimeout.TotalSeconds);
            ModelState.AddModelError(string.Empty, "O banco demorou para responder. Tente novamente.");
            await CarregarRegistroAsync(Input.RegistroId, cancellationToken: cancellationToken);
            return Page();
        }
        catch (RetryLimitExceededException exception)
        {
            _logger.LogError(
                exception,
                "Vehicle exit registration exhausted database retries. {RegistrationId}",
                Input.RegistroId);
            ModelState.AddModelError(string.Empty, "Não foi possível registrar a saída agora. Tente novamente.");
            await CarregarRegistroAsync(Input.RegistroId, cancellationToken: cancellationToken);
            return Page();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Vehicle exit registration failed while saving. {RegistrationId}",
                Input.RegistroId);
            ModelState.AddModelError(string.Empty, "Não foi possível salvar a saída. Tente novamente.");
            await CarregarRegistroAsync(Input.RegistroId, cancellationToken: cancellationToken);
            return Page();
        }

        if (registroNaoEncontrado)
        {
            ModelState.AddModelError(string.Empty, "Registro não encontrado ou veículo já liberado.");
            await CarregarRegistroAsync(Input.RegistroId, cancellationToken: cancellationToken);
            return Page();
        }

        TempData[NotificacaoChaves.Sucesso] = "Saída registrada. O veículo foi liberado com sucesso.";
        return RedirectToPage("/Veiculos/RelatorioDoDia");
    }

    private void ValidarServicos()
    {
        if (Input.RegistroId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Registro não encontrado.");
        }

        if (!Input.ConfirmarSaida)
        {
            ModelState.AddModelError("Input.ConfirmarSaida", "Confirme a saída do veículo.");
        }

        if (Input.FezConserto)
        {
            if (string.IsNullOrWhiteSpace(Input.DescricaoConserto))
            {
                ModelState.AddModelError("Input.DescricaoConserto", "Descreva o conserto realizado.");
            }

            if (!Input.ValorConserto.HasValue || Input.ValorConserto.Value <= 0)
            {
                ModelState.AddModelError("Input.ValorConserto", "Informe o valor do conserto.");
            }
        }
        else
        {
            Input.DescricaoConserto = null;
            Input.ValorConserto = null;
        }

        if (Input.FezSelagem)
        {
            if (!Input.CategoriaTarifa.HasValue)
            {
                ModelState.AddModelError("Input.CategoriaTarifa", "Escolha o tipo de veículo para calcular a tarifa.");
            }

            if (!Input.IncluiGru.HasValue)
            {
                ModelState.AddModelError("Input.IncluiGru", "Informe se a tarifa inclui GRU.");
            }
        }
        else
        {
            Input.CategoriaTarifa = null;
            Input.IncluiGru = null;
        }
    }

    private async Task<bool> CarregarRegistroAsync(
        int id,
        bool inicializarInput = false,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            RegistroVeiculo = new RegistroVeiculo();
            return false;
        }

        var registro = await _context.RegistrosVeiculos
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (registro is null)
        {
            RegistroVeiculo = new RegistroVeiculo();
            return false;
        }

        RegistroVeiculo = registro;
        RegistroVeiculo.RestaurarFlagsDeServicos();

        if (inicializarInput)
        {
            Input = new SaidaInput
            {
                RegistroId = registro.Id,
                FezConserto = registro.FezConserto,
                FezSelagem = registro.FezSelagem,
                DescricaoConserto = registro.DescricaoConserto,
                TecnicoResponsavel = registro.TecnicoResponsavel
            };
        }

        return true;
    }

    public sealed class SaidaInput
    {
        [Range(1, int.MaxValue)]
        public int RegistroId { get; set; }

        public bool FezConserto { get; set; }

        [StringLength(200, ErrorMessage = "A descrição deve ter no máximo 200 caracteres.")]
        public string? DescricaoConserto { get; set; }

        [Range(
            typeof(decimal),
            "0.01",
            "999999999.99",
            ParseLimitsInInvariantCulture = true,
            ErrorMessage = "Informe um valor de conserto válido.")]
        public decimal? ValorConserto { get; set; }

        public bool FezSelagem { get; set; }

        public CategoriaTarifa? CategoriaTarifa { get; set; }

        public bool? IncluiGru { get; set; }

        [StringLength(100, ErrorMessage = "O nome do técnico deve ter no máximo 100 caracteres.")]
        public string? TecnicoResponsavel { get; set; }

        public bool ConfirmarSaida { get; set; }
    }
}
