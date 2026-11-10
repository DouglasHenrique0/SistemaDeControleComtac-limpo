using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaDeControleComtac.Models;

namespace SistemaDeControleComtac.Data;

public sealed class SqlServerSessionContextInterceptor(
    IHttpContextAccessor httpContextAccessor) : DbConnectionInterceptor
{
    private const string SetContextCommand = """
        EXEC sys.sp_set_session_context
            @key = N'comtac_is_admin',
            @value = @is_admin,
            @read_only = 0;
        """;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (connection is SqlConnection)
        {
            SetSessionContext(connection);
        }
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is SqlConnection)
        {
            await SetSessionContextAsync(connection, cancellationToken);
        }
    }

    private void SetSessionContext(DbConnection connection)
    {
        using var command = CreateCommand(connection);
        command.ExecuteNonQuery();
    }

    private async Task SetSessionContextAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = SetContextCommand;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@is_admin";
        parameter.DbType = DbType.Boolean;
        parameter.Value = IsAdministrativeContext();
        command.Parameters.Add(parameter);

        return command;
    }

    private bool IsAdministrativeContext()
    {
        var httpContext = httpContextAccessor.HttpContext;

        // Processos internos de startup/migration não têm request e precisam administrar o schema.
        return httpContext is null || httpContext.User.IsInRole(ApplicationRoles.Admin);
    }
}
