using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Ditado.Infra.Data;

#nullable disable

namespace Ditado.Infra.Migrations;

[DbContext(typeof(DitadoDbContext))]
[Migration("20261107123000_AdicionarEstadoPinAluno")]
public partial class AdicionarEstadoPinAluno : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<bool>(name: "PinConfigurado", table: "Usuarios", type: "tinyint(1)", nullable: false, defaultValue: false);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "PinConfigurado", table: "Usuarios");
}
