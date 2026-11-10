using Microsoft.Extensions.Caching.Memory;
using SistemaDeControleComtac.Infrastructure;

namespace SistemaDeControleComtac.Tests;

public sealed class InstrumentedMemoryCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ReutilizaValorDentroDaValidade()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new InstrumentedMemoryCache(memoryCache);
        var factoryCalls = 0;

        Task<int> Factory(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult(42);
        }

        var first = await cache.GetOrCreateAsync("answer", TimeSpan.FromMinutes(1), Factory);
        var second = await cache.GetOrCreateAsync("answer", TimeSpan.FromMinutes(1), Factory);

        Assert.Equal(42, first);
        Assert.Equal(42, second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_RejeitaDuracaoInvalida()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new InstrumentedMemoryCache(memoryCache);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            cache.GetOrCreateAsync("answer", TimeSpan.Zero, _ => Task.FromResult(42)));
    }
}
