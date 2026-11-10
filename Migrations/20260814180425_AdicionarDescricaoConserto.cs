using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDeControleComtac.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDescricaoConserto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescricaoConserto",
                table: "RegistrosVeiculos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FezConserto",
                table: "RegistrosVeiculos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FezEnsaio",
                table: "RegistrosVeiculos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FezSelagem",
                table: "RegistrosVeiculos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescricaoConserto",
                table: "RegistrosVeiculos");

            migrationBuilder.DropColumn(
                name: "FezConserto",
                table: "RegistrosVeiculos");

            migrationBuilder.DropColumn(
                name: "FezEnsaio",
                table: "RegistrosVeiculos");

            migrationBuilder.DropColumn(
                name: "FezSelagem",
                table: "RegistrosVeiculos");
        }
    }
}
