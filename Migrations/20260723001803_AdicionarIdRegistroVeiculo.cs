using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDeControleComtac.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarIdRegistroVeiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ServicosRealizados",
                table: "RegistrosVeiculos",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "RegistrosVeiculos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Empresa",
                table: "RegistrosVeiculos");

            migrationBuilder.AlterColumn<string>(
                name: "ServicosRealizados",
                table: "RegistrosVeiculos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
