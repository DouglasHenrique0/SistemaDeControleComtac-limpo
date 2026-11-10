using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using SistemaDeControleComtac.Observability;

namespace SistemaDeControleComtac.Infrastructure;

public sealed class InstrumentedMemoryCache(IMemoryCache cache) : IApplicationCache
{
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime), "A duração do cache deve ser positiva.");
        }

        if (cache.TryGetValue<CacheEnvelope<T>>(key, out var envelope) && envelope is not null)
        {
            Telemetry.CacheOperations.Add(1, new KeyValuePair<string, object?>("outcome", "hit"));
            return envelope.Value;
        }

        Telemetry.CacheOperations.Add(1, new KeyValuePair<string, object?>("outcome", "miss"));
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var value = await factory(cancellationToken);
            cache.Set(key, new CacheEnvelope<T>(value), lifetime);
            Telemetry.CacheFactoryDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return value;
        }
        catch
        {
            Telemetry.CacheOperations.Add(1, new KeyValuePair<string, object?>("outcome", "error"));
            throw;
        }
    }

    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cache.Remove(key);
    }

    private sealed record CacheEnvelope<T>(T Value);
}
