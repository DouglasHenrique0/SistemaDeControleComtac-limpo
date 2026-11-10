using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDeControleComtac.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosVeiculos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NumeroDoDia = table.Column<int>(type: "INTEGER", nullable: false),
                    OSComtac = table.Column<string>(type: "TEXT", nullable: false),
                    Placa = table.Column<string>(type: "TEXT", nullable: false),
                    Frota = table.Column<string>(type: "TEXT", nullable: true),
                    Cliente = table.Column<string>(type: "TEXT", nullable: false),
                    ModeloTCO = table.Column<string>(type: "TEXT", nullable: false),
                    NumeroSerie = table.Column<string>(type: "TEXT", nullable: false),
                    DataHoraEntrada = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ServicosRealizados = table.Column<string>(type: "TEXT", nullable: true),
                    AtividadesRealizadas = table.Column<string>(type: "TEXT", nullable: true),
                    TecnicoResponsavel = table.Column<string>(type: "TEXT", nullable: true),
                    DataHoraSaida = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosVeiculos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosVeiculos");
        }
    }
}
