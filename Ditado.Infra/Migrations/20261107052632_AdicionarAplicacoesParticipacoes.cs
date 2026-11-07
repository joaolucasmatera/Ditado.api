using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAplicacoesParticipacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AplicacaoDitadoId",
                table: "RespostasDitados",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscolaId",
                table: "RespostasDitados",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AplicacoesDitados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EscolaId = table.Column<int>(type: "int", nullable: false),
                    TurmaId = table.Column<int>(type: "int", nullable: false),
                    DitadoId = table.Column<int>(type: "int", nullable: false),
                    DataAtribuicao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataLimite = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacoesDitados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AplicacoesDitados_Ditados_DitadoId",
                        column: x => x.DitadoId,
                        principalTable: "Ditados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AplicacoesDitados_Escolas_EscolaId",
                        column: x => x.EscolaId,
                        principalTable: "Escolas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AplicacoesDitados_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ParticipacoesTurmas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TurmaId = table.Column<int>(type: "int", nullable: false),
                    AlunoId = table.Column<int>(type: "int", nullable: false),
                    DataEntrada = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataSaida = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipacoesTurmas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipacoesTurmas_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipacoesTurmas_Usuarios_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AplicacoesErrosCatalogo",
                columns: table => new
                {
                    AplicacaoDitadoId = table.Column<int>(type: "int", nullable: false),
                    ErroCatalogoId = table.Column<int>(type: "int", nullable: false),
                    CodigoSnapshot = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomeSnapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacoesErrosCatalogo", x => new { x.AplicacaoDitadoId, x.ErroCatalogoId });
                    table.ForeignKey(
                        name: "FK_AplicacoesErrosCatalogo_AplicacoesDitados_AplicacaoDitadoId",
                        column: x => x.AplicacaoDitadoId,
                        principalTable: "AplicacoesDitados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AplicacoesErrosCatalogo_ErrosCatalogo_ErroCatalogoId",
                        column: x => x.ErroCatalogoId,
                        principalTable: "ErrosCatalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ParticipacoesAplicacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AplicacaoDitadoId = table.Column<int>(type: "int", nullable: false),
                    AlunoId = table.Column<int>(type: "int", nullable: false),
                    ConcedidaEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevogadaEm = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipacoesAplicacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipacoesAplicacoes_AplicacoesDitados_AplicacaoDitadoId",
                        column: x => x.AplicacaoDitadoId,
                        principalTable: "AplicacoesDitados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParticipacoesAplicacoes_Usuarios_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RespostasDitados_AplicacaoDitadoId_AlunoId_DataRealizacao",
                table: "RespostasDitados",
                columns: new[] { "AplicacaoDitadoId", "AlunoId", "DataRealizacao" });

            migrationBuilder.CreateIndex(
                name: "IX_AplicacoesDitados_DitadoId",
                table: "AplicacoesDitados",
                column: "DitadoId");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacoesDitados_EscolaId_Ativa",
                table: "AplicacoesDitados",
                columns: new[] { "EscolaId", "Ativa" });

            migrationBuilder.CreateIndex(
                name: "IX_AplicacoesDitados_TurmaId_DitadoId",
                table: "AplicacoesDitados",
                columns: new[] { "TurmaId", "DitadoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AplicacoesErrosCatalogo_ErroCatalogoId",
                table: "AplicacoesErrosCatalogo",
                column: "ErroCatalogoId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacoesAplicacoes_AlunoId_RevogadaEm",
                table: "ParticipacoesAplicacoes",
                columns: new[] { "AlunoId", "RevogadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacoesAplicacoes_AplicacaoDitadoId_AlunoId",
                table: "ParticipacoesAplicacoes",
                columns: new[] { "AplicacaoDitadoId", "AlunoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacoesTurmas_AlunoId_Ativa",
                table: "ParticipacoesTurmas",
                columns: new[] { "AlunoId", "Ativa" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacoesTurmas_TurmaId_AlunoId",
                table: "ParticipacoesTurmas",
                columns: new[] { "TurmaId", "AlunoId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RespostasDitados_AplicacoesDitados_AplicacaoDitadoId",
                table: "RespostasDitados",
                column: "AplicacaoDitadoId",
                principalTable: "AplicacoesDitados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RespostasDitados_AplicacoesDitados_AplicacaoDitadoId",
                table: "RespostasDitados");

            migrationBuilder.DropTable(
                name: "AplicacoesErrosCatalogo");

            migrationBuilder.DropTable(
                name: "ParticipacoesAplicacoes");

            migrationBuilder.DropTable(
                name: "ParticipacoesTurmas");

            migrationBuilder.DropTable(
                name: "AplicacoesDitados");

            migrationBuilder.DropIndex(
                name: "IX_RespostasDitados_AplicacaoDitadoId_AlunoId_DataRealizacao",
                table: "RespostasDitados");

            migrationBuilder.DropColumn(
                name: "AplicacaoDitadoId",
                table: "RespostasDitados");

            migrationBuilder.DropColumn(
                name: "EscolaId",
                table: "RespostasDitados");
        }
    }
}
