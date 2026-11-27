using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ditado.Infra.Migrations;

[DbContext(typeof(DitadoDbContext))]
[Migration("20261124120000_AdicionarExemplosAoCatalogo")]
public sealed class AdicionarExemplosAoCatalogo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("ExemplosAdicionaisJson", "ErrosCatalogo", "longtext", nullable: true);
        migrationBuilder.AddColumn<string>("ExemplosAdicionaisJson", "LlmAnalisesCandidatos", "longtext", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("ExemplosAdicionaisJson", "ErrosCatalogo");
        migrationBuilder.DropColumn("ExemplosAdicionaisJson", "LlmAnalisesCandidatos");
    }
}
