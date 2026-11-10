using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDeControleComtac.Migrations
{
    /// <inheritdoc />
    public partial class FortalecerSegurancaFinanceiroEConsultas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE AspNetUsers SET LockoutEnabled = 1;");

            migrationBuilder.Sql(
                "UPDATE RegistrosVeiculos SET Status = 'No pátio' WHERE Status IS NULL OR trim(Status) = '' OR trim(Status, '. ') = 'No pátio';");

            migrationBuilder.CreateTable(
                name: "Receitas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegistroVeiculoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receitas_RegistrosVeiculos_RegistroVeiculoId",
                        column: x => x.RegistroVeiculoId,
                        principalTable: "RegistrosVeiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosVeiculos_DataHoraEntrada",
                table: "RegistrosVeiculos",
                column: "DataHoraEntrada");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_Data",
                table: "Receitas",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_Receitas_RegistroVeiculoId",
                table: "Receitas",
                column: "RegistroVeiculoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Receitas");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosVeiculos_DataHoraEntrada",
                table: "RegistrosVeiculos");
        }
    }
}
