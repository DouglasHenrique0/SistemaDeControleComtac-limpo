using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDeControleComtac.SqlServerMigrations
{
    /// <inheritdoc />
    public partial class EnableFinanceRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE FUNCTION [dbo].[fn_ComtacFinanceAccess](@row_id int)
                RETURNS TABLE
                WITH SCHEMABINDING
                AS
                RETURN
                (
                    SELECT 1 AS [access_result]
                    WHERE TRY_CONVERT(bit, SESSION_CONTEXT(N'comtac_is_admin')) = 1
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE SECURITY POLICY [dbo].[ComtacFinanceSecurityPolicy]
                    ADD FILTER PREDICATE [dbo].[fn_ComtacFinanceAccess]([Id]) ON [dbo].[Receitas],
                    ADD FILTER PREDICATE [dbo].[fn_ComtacFinanceAccess]([Id]) ON [dbo].[Despesas],
                    ADD FILTER PREDICATE [dbo].[fn_ComtacFinanceAccess]([Id]) ON [dbo].[RelatoriosExcelGerados]
                WITH (STATE = ON, SCHEMABINDING = ON);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP SECURITY POLICY IF EXISTS [dbo].[ComtacFinanceSecurityPolicy];
                DROP FUNCTION IF EXISTS [dbo].[fn_ComtacFinanceAccess];
                """);
        }
    }
}
