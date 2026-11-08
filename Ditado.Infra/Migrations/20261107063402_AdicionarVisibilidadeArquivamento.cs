using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarVisibilidadeArquivamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArquivadoEm",
                table: "ErrosCatalogo",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArquivadoPorId",
                table: "ErrosCatalogo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArquivadoEm",
                table: "Ditados",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArquivadoPorId",
                table: "Ditados",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PublicacoesDitados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DitadoId = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    EscolaId = table.Column<int>(type: "int", nullable: true),
                    Versao = table.Column<int>(type: "int", nullable: false),
                    ChaveVigente = table.Column<int>(type: "int", nullable: true),
                    VigenteDesde = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EncerradaEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CriadaPorId = table.Column<int>(type: "int", nullable: false),
                    EncerradaPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicacoesDitados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicacoesDitados_Ditados_DitadoId",
                        column: x => x.DitadoId,
                        principalTable: "Ditados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicacoesDitados_Escolas_EscolaId",
                        column: x => x.EscolaId,
                        principalTable: "Escolas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PublicacoesErrosCatalogo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ErroCatalogoId = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    EscolaId = table.Column<int>(type: "int", nullable: true),
                    Versao = table.Column<int>(type: "int", nullable: false),
                    ChaveVigente = table.Column<int>(type: "int", nullable: true),
                    VigenteDesde = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EncerradaEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CriadaPorId = table.Column<int>(type: "int", nullable: false),
                    EncerradaPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicacoesErrosCatalogo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicacoesErrosCatalogo_ErrosCatalogo_ErroCatalogoId",
                        column: x => x.ErroCatalogoId,
                        principalTable: "ErrosCatalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicacoesErrosCatalogo_Escolas_EscolaId",
                        column: x => x.EscolaId,
                        principalTable: "Escolas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PublicacoesDitadosTurmas",
                columns: table => new
                {
                    PublicacaoDitadoId = table.Column<int>(type: "int", nullable: false),
                    TurmaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicacoesDitadosTurmas", x => new { x.PublicacaoDitadoId, x.TurmaId });
                    table.ForeignKey(
                        name: "FK_PublicacoesDitadosTurmas_PublicacoesDitados_PublicacaoDitado~",
                        column: x => x.PublicacaoDitadoId,
                        principalTable: "PublicacoesDitados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicacoesDitadosTurmas_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PublicacoesErrosCatalogoTurmas",
                columns: table => new
                {
                    PublicacaoErroCatalogoId = table.Column<int>(type: "int", nullable: false),
                    TurmaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicacoesErrosCatalogoTurmas", x => new { x.PublicacaoErroCatalogoId, x.TurmaId });
                    table.ForeignKey(
                        name: "FK_PublicacoesErrosCatalogoTurmas_PublicacoesErrosCatalogo_Publ~",
                        column: x => x.PublicacaoErroCatalogoId,
                        principalTable: "PublicacoesErrosCatalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicacoesErrosCatalogoTurmas_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PublicacoesDitados_DitadoId_ChaveVigente",
                table: "PublicacoesDitados",
                columns: new[] { "DitadoId", "ChaveVigente" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicacoesDitados_EscolaId_Nivel_EncerradaEm",
                table: "PublicacoesDitados",
                columns: new[] { "EscolaId", "Nivel", "EncerradaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicacoesDitadosTurmas_TurmaId",
                table: "PublicacoesDitadosTurmas",
                column: "TurmaId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicacoesErrosCatalogo_ErroCatalogoId_ChaveVigente",
                table: "PublicacoesErrosCatalogo",
                columns: new[] { "ErroCatalogoId", "ChaveVigente" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicacoesErrosCatalogo_EscolaId_Nivel_EncerradaEm",
                table: "PublicacoesErrosCatalogo",
                columns: new[] { "EscolaId", "Nivel", "EncerradaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicacoesErrosCatalogoTurmas_TurmaId",
                table: "PublicacoesErrosCatalogoTurmas",
                column: "TurmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicacoesDitadosTurmas");

            migrationBuilder.DropTable(
                name: "PublicacoesErrosCatalogoTurmas");

            migrationBuilder.DropTable(
                name: "PublicacoesDitados");

            migrationBuilder.DropTable(
                name: "PublicacoesErrosCatalogo");

            migrationBuilder.DropColumn(
                name: "ArquivadoEm",
                table: "ErrosCatalogo");

            migrationBuilder.DropColumn(
                name: "ArquivadoPorId",
                table: "ErrosCatalogo");

            migrationBuilder.DropColumn(
                name: "ArquivadoEm",
                table: "Ditados");

            migrationBuilder.DropColumn(
                name: "ArquivadoPorId",
                table: "Ditados");
        }
    }
}
