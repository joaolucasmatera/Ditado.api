using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ditado.Infra.Migrations;

[DbContext(typeof(DitadoDbContext))]
[Migration("20261125120000_AdicionarCategoriaPedagogicaAosAchados")]
public sealed class AdicionarCategoriaPedagogicaAosAchados : Migration
{
    private static readonly string[] Tabelas = ["RespostaSegmentoTiposErro", "LlmAnalisesCandidatos"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var tabela in Tabelas)
        {
            migrationBuilder.AddColumn<string>("CategoriaPedagogicaCodigo", tabela, "varchar(40)",
                maxLength: 40, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>("CategoriaPedagogicaNome", tabela, "varchar(200)",
                maxLength: 200, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
            migrationBuilder.AddColumn<string>("CategoriaPedagogicaFamilia", tabela, "varchar(100)",
                maxLength: 100, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var tabela in Tabelas)
        {
            migrationBuilder.DropColumn("CategoriaPedagogicaCodigo", tabela);
            migrationBuilder.DropColumn("CategoriaPedagogicaNome", tabela);
            migrationBuilder.DropColumn("CategoriaPedagogicaFamilia", tabela);
        }
    }
}
