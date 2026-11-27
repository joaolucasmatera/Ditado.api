using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ditado.Infra.Migrations;

[DbContext(typeof(DitadoDbContext))]
[Migration("20261123120000_CongelarMetadadosRouterPer69")]
public sealed class CongelarMetadadosRouterPer69 : Migration
{
    private const string Tabela = "LlmAnalisesCandidatos";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("ExemploEsperado", Tabela, "longtext", nullable: true);
        migrationBuilder.AddColumn<string>("ExemploResposta", Tabela, "longtext", nullable: true);
        migrationBuilder.AddColumn<string>("ObservacaoPedagogica", Tabela, "longtext", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("ExemploEsperado", Tabela);
        migrationBuilder.DropColumn("ExemploResposta", Tabela);
        migrationBuilder.DropColumn("ObservacaoPedagogica", Tabela);
    }
}
