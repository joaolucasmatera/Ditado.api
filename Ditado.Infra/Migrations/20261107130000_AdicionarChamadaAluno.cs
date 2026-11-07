using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations;

public partial class AdicionarChamadaAluno : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<int>(name: "NumeroChamada", table: "Usuarios", type: "int", nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "NumeroChamada", table: "Usuarios");
}
