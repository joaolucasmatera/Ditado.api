using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Ditado.Infra.Migrations;

[DbContext(typeof(DitadoDbContext))]
[Migration("20261124233000_AdicionarCategoriaPedagogicaAoErro")]
public sealed class AdicionarCategoriaPedagogicaAoErro : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("CategoriaPedagogicaCodigo", "ErrosCatalogo", "varchar(40)",
            maxLength: 40, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AddColumn<string>("CategoriaPedagogicaNome", "ErrosCatalogo", "varchar(200)",
            maxLength: 200, nullable: true).Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("CategoriaPedagogicaCodigo", "ErrosCatalogo");
        migrationBuilder.DropColumn("CategoriaPedagogicaNome", "ErrosCatalogo");
    }
}
