namespace SistemaDeControleComtac.Infrastructure;

public interface IApplicationCache
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);

    void Remove(string key);
}
