using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDeControleComtac.Migrations
{
    /// <inheritdoc />
    public partial class CompletarRelatoriosExcelMensais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataRecebimento",
                table: "Receitas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Destinatario",
                table: "Receitas",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "Receitas",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nota",
                table: "Receitas",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pagamento",
                table: "Receitas",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusFinanceiro",
                table: "Receitas",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Pendente");

            migrationBuilder.CreateTable(
                name: "Despesas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Pagamento = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Despesas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelatoriosExcelGerados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Mes = table.Column<int>(type: "INTEGER", nullable: false),
                    Ano = table.Column<int>(type: "INTEGER", nullable: false),
                    NomeArquivo = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    CaminhoRelativo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    DataGeracao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioGeradorId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatoriosExcelGerados", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Despesas_Data",
                table: "Despesas",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_RelatoriosExcelGerados_Ano_Mes_DataGeracao",
                table: "RelatoriosExcelGerados",
                columns: new[] { "Ano", "Mes", "DataGeracao" });

            migrationBuilder.CreateIndex(
                name: "IX_RelatoriosExcelGerados_CaminhoRelativo",
                table: "RelatoriosExcelGerados",
                column: "CaminhoRelativo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Despesas");

            migrationBuilder.DropTable(
                name: "RelatoriosExcelGerados");

            migrationBuilder.DropColumn(
                name: "DataRecebimento",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "Destinatario",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "Documento",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "Nota",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "Pagamento",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "StatusFinanceiro",
                table: "Receitas");
        }
    }
}
