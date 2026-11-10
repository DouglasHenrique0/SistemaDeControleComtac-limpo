using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SistemaDeControleComtac.Health;

public sealed class ReportStorageHealthCheck(IWebHostEnvironment environment) : IHealthCheck
{
    private readonly string _storagePath = Path.GetFullPath(
        Path.Combine(environment.ContentRootPath, "App_Data", "RelatoriosExcel"));

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_storagePath);
            var probePath = Path.Combine(_storagePath, $".health-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(probePath, "ok", cancellationToken);
            File.Delete(probePath);

            return HealthCheckResult.Healthy("Armazenamento de relatórios disponível.");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return HealthCheckResult.Unhealthy(
                "Armazenamento de relatórios indisponível.",
                exception);
        }
    }
}
