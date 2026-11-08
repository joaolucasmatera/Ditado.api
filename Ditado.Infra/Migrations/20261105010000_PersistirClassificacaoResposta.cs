using Ditado.Infra.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations;

public partial class PersistirClassificacaoResposta : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RespostaEsperada",
            table: "RespostaSegmentos",
            type: "varchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MetodoClassificacao",
            table: "RespostaSegmentos",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "EstadoClassificacao",
            table: "RespostaSegmentos",
            type: "int",
            nullable: false,
            defaultValue: 2);

        migrationBuilder.Sql(
            "UPDATE RespostaSegmentos SET EstadoClassificacao = 0 WHERE Correto = 1;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RespostaEsperada", table: "RespostaSegmentos");
        migrationBuilder.DropColumn(name: "MetodoClassificacao", table: "RespostaSegmentos");
        migrationBuilder.DropColumn(name: "EstadoClassificacao", table: "RespostaSegmentos");
    }
}
