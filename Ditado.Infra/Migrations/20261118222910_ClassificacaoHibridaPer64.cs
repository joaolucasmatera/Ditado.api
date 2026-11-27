using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations
{
    /// <inheritdoc />
    public partial class ClassificacaoHibridaPer64 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RespostaAusente",
                table: "RespostaSegmentos",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "LlmAnalisesTarefas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RespostaSegmentoId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Tentativas = table.Column<int>(type: "int", nullable: false),
                    PromptVersao = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Provedor = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Modelo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Origem = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UltimoErro = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadaEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IniciadaEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConcluidaEm = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmAnalisesTarefas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LlmAnalisesTarefas_RespostaSegmentos_RespostaSegmentoId",
                        column: x => x.RespostaSegmentoId,
                        principalTable: "RespostaSegmentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LlmAnalisesCandidatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LlmAnaliseTarefaId = table.Column<int>(type: "int", nullable: false),
                    ErroCatalogoId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "text", maxLength: 10000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Versao = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Confianca = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    Evidencia = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvaliadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmAnalisesCandidatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LlmAnalisesCandidatos_ErrosCatalogo_ErroCatalogoId",
                        column: x => x.ErroCatalogoId,
                        principalTable: "ErrosCatalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LlmAnalisesCandidatos_LlmAnalisesTarefas_LlmAnaliseTarefaId",
                        column: x => x.LlmAnaliseTarefaId,
                        principalTable: "LlmAnalisesTarefas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LlmAnalisesCandidatos_ErroCatalogoId",
                table: "LlmAnalisesCandidatos",
                column: "ErroCatalogoId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmAnalisesCandidatos_LlmAnaliseTarefaId_ErroCatalogoId",
                table: "LlmAnalisesCandidatos",
                columns: new[] { "LlmAnaliseTarefaId", "ErroCatalogoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LlmAnalisesTarefas_Estado_CriadaEm",
                table: "LlmAnalisesTarefas",
                columns: new[] { "Estado", "CriadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_LlmAnalisesTarefas_RespostaSegmentoId",
                table: "LlmAnalisesTarefas",
                column: "RespostaSegmentoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LlmAnalisesCandidatos");

            migrationBuilder.DropTable(
                name: "LlmAnalisesTarefas");

            migrationBuilder.DropColumn(
                name: "RespostaAusente",
                table: "RespostaSegmentos");
        }
    }
}
