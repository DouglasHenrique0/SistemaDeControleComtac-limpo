using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaDeControleComtac.Data;

namespace SistemaDeControleComtac.Tests;

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestDatabase(SqliteConnection connection, AppDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new TestDatabase(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
