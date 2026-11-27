using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ditado.Infra.Migrations;

[DbContext(typeof(DitadoDbContext))]
[Migration("20261122120000_RegistrarTentativasRouterPer69")]
public sealed class RegistrarTentativasRouterPer69 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "TentativasRouter", table: "LlmTentativasProvedor", type: "int", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TentativasRouter", table: "LlmTentativasProvedor");
    }
}
