using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SistemaDeControleComtac.Observability;

public static class Telemetry
{
    public const string ActivitySourceName = "SistemaDeControleComtac";
    public const string MeterName = "SistemaDeControleComtac";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Histogram<double> DatabaseQueryDuration = Meter.CreateHistogram<double>(
        "comtac.database.query.duration",
        unit: "ms",
        description: "Tempo de execução de comandos no banco de dados.");

    public static readonly Counter<long> DatabaseQueryFailures = Meter.CreateCounter<long>(
        "comtac.database.query.failures",
        unit: "{failure}",
        description: "Quantidade de falhas de comandos no banco de dados.");

    public static readonly Counter<long> LoginAttempts = Meter.CreateCounter<long>(
        "comtac.auth.login.attempts",
        unit: "{attempt}",
        description: "Tentativas de login classificadas por resultado.");

    public static readonly Counter<long> CacheOperations = Meter.CreateCounter<long>(
        "comtac.cache.operations",
        unit: "{operation}",
        description: "Operações de cache classificadas como hit, miss ou error.");

    public static readonly Histogram<double> CacheFactoryDuration = Meter.CreateHistogram<double>(
        "comtac.cache.factory.duration",
        unit: "ms",
        description: "Tempo necessário para produzir um valor ausente no cache.");
}
