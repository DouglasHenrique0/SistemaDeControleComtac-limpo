using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDeControleComtac.Migrations
{
    /// <inheritdoc />
    public partial class Versao1RelatorioFinanceiroMensal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receitas_RegistroVeiculoId",
                table: "Receitas");

            migrationBuilder.AddColumn<string>(
                name: "CategoriaTarifa",
                table: "Receitas",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncluiGru",
                table: "Receitas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Receitas",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Outro");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_RegistroVeiculoId_Tipo",
                table: "Receitas",
                columns: new[] { "RegistroVeiculoId", "Tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receitas_RegistroVeiculoId_Tipo",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "CategoriaTarifa",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "IncluiGru",
                table: "Receitas");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Receitas");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_RegistroVeiculoId",
                table: "Receitas",
                column: "RegistroVeiculoId",
                unique: true);
        }
    }
}
