using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Ditado.Infra.Data;

#nullable disable

namespace Ditado.Infra.Migrations;

[DbContext(typeof(DitadoDbContext))]
[Migration("20261107130000_AdicionarChamadaAluno")]
public partial class AdicionarChamadaAluno : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<int>(name: "NumeroChamada", table: "Usuarios", type: "int", nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "NumeroChamada", table: "Usuarios");
}
