using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations;

public partial class AdicionarEstadoPinAluno : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<bool>(name: "PinConfigurado", table: "Usuarios", type: "tinyint(1)", nullable: false, defaultValue: false);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "PinConfigurado", table: "Usuarios");
}
