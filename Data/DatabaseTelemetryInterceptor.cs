using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaDeControleComtac.Observability;

namespace SistemaDeControleComtac.Data;

public sealed class DatabaseTelemetryInterceptor(
    ILogger<DatabaseTelemetryInterceptor> logger) : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        RecordSuccess(command, eventData);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        RecordSuccess(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        RecordSuccess(command, eventData);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        RecordSuccess(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        RecordSuccess(command, eventData);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        RecordSuccess(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        RecordFailure(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RecordFailure(command, eventData);
        return Task.CompletedTask;
    }

    private void RecordSuccess(DbCommand command, CommandExecutedEventData eventData)
    {
        var durationMs = eventData.Duration.TotalMilliseconds;
        var provider = command.Connection?.GetType().Name ?? "unknown";
        Telemetry.DatabaseQueryDuration.Record(
            durationMs,
            new KeyValuePair<string, object?>("db.provider", provider),
            new KeyValuePair<string, object?>("db.outcome", "success"));

        logger.LogInformation(
            "Database command completed. {DurationMs} {DatabaseProvider} {CommandType} {QueryFingerprint}",
            durationMs,
            provider,
            command.CommandType.ToString(),
            CreateFingerprint(command.CommandText));
    }

    private void RecordFailure(DbCommand command, CommandErrorEventData eventData)
    {
        var durationMs = eventData.Duration.TotalMilliseconds;
        var provider = command.Connection?.GetType().Name ?? "unknown";
        Telemetry.DatabaseQueryDuration.Record(
            durationMs,
            new KeyValuePair<string, object?>("db.provider", provider),
            new KeyValuePair<string, object?>("db.outcome", "failure"));
        Telemetry.DatabaseQueryFailures.Add(
            1,
            new KeyValuePair<string, object?>("db.provider", provider));

        logger.LogError(
            eventData.Exception,
            "Database command failed. {DurationMs} {DatabaseProvider} {CommandType} {QueryFingerprint}",
            durationMs,
            provider,
            command.CommandType.ToString(),
            CreateFingerprint(command.CommandText));
    }

    private static string CreateFingerprint(string commandText)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(commandText));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}
