using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations
{
    /// <inheritdoc />
    public partial class IntegrarCatalogoCor01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErroCatalogoId",
                table: "RespostaSegmentoTiposErro",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegraClassificacao",
                table: "ErrosCatalogo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UtilizadoEm",
                table: "ErrosCatalogo",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscolaSelecaoErrosId",
                table: "Ditados",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DitadosErrosCatalogo",
                columns: table => new
                {
                    DitadoId = table.Column<int>(type: "int", nullable: false),
                    ErroCatalogoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DitadosErrosCatalogo", x => new { x.DitadoId, x.ErroCatalogoId });
                    table.ForeignKey(
                        name: "FK_DitadosErrosCatalogo_Ditados_DitadoId",
                        column: x => x.DitadoId,
                        principalTable: "Ditados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DitadosErrosCatalogo_ErrosCatalogo_ErroCatalogoId",
                        column: x => x.ErroCatalogoId,
                        principalTable: "ErrosCatalogo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RespostaSegmentoTiposErro_ErroCatalogoId",
                table: "RespostaSegmentoTiposErro",
                column: "ErroCatalogoId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrosCatalogo_RegraClassificacao",
                table: "ErrosCatalogo",
                column: "RegraClassificacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ditados_EscolaSelecaoErrosId",
                table: "Ditados",
                column: "EscolaSelecaoErrosId");

            migrationBuilder.CreateIndex(
                name: "IX_DitadosErrosCatalogo_ErroCatalogoId",
                table: "DitadosErrosCatalogo",
                column: "ErroCatalogoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ditados_Escolas_EscolaSelecaoErrosId",
                table: "Ditados",
                column: "EscolaSelecaoErrosId",
                principalTable: "Escolas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                INSERT INTO Usuarios (Nome, Login, SenhaHash, Tipo, Ativo, DataCriacao)
                VALUES ('Catálogo de erros do Ditado', 'catalogo-regras@ditado.invalid', 'SEM_ACESSO', 1, FALSE, UTC_TIMESTAMP(6))
                ON DUPLICATE KEY UPDATE Id = Id;
                """);
            var regras = new (int Tipo, string Codigo, string Nome)[]
            {
                (2, "ACENTUACAO", "Acentuação"),
                (19, "SURDA_SONORA", "Troca de grafemas surda/sonora"),
                (20, "REGRA_CONTEXTUAL", "Regra contextual"),
                (21, "GRAFIA_ARBITRARIA", "Grafia arbitrária"),
                (22, "NASALIZACAO", "Nasalização"),
                (24, "COMPLEXIDADE_SILABICA", "Complexidade silábica"),
                (26, "JUNTURA_SEGMENTACAO", "Juntura e segmentação"),
                (27, "CONCORDANCIA_FLEXAO", "Concordância e flexão"),
                (28, "MAIUSCULAS_PONTUACAO", "Maiúsculas e pontuação"),
                (29, "OMISSAO", "Omissão"),
                (30, "ACRESCIMO", "Acréscimo"),
                (31, "SUBSTITUICAO", "Substituição")
            };
            foreach (var (tipo, codigo, nome) in regras)
            {
                migrationBuilder.Sql($"""
                    INSERT INTO ErrosCatalogo (Codigo, Nome, Descricao, AutorId, Ativo, DataCriacao, DataAlteracao, RegraClassificacao)
                    SELECT 'COR01_{codigo}', '{nome}', 'Regra programada de correção da escrita.', Id, TRUE, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), {tipo}
                    FROM Usuarios WHERE Login = 'catalogo-regras@ditado.invalid'
                    ON DUPLICATE KEY UPDATE RegraClassificacao = VALUES(RegraClassificacao);
                    """);
            }
            migrationBuilder.Sql("""
                INSERT INTO PublicacoesErrosCatalogo (ErroCatalogoId, Nivel, Versao, ChaveVigente, VigenteDesde, CriadaPorId)
                SELECT e.Id, 1, 1, 1, UTC_TIMESTAMP(6), e.AutorId
                FROM ErrosCatalogo e
                WHERE e.RegraClassificacao IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM PublicacoesErrosCatalogo p WHERE p.ErroCatalogoId = e.Id);
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_RespostaSegmentoTiposErro_ErrosCatalogo_ErroCatalogoId",
                table: "RespostaSegmentoTiposErro",
                column: "ErroCatalogoId",
                principalTable: "ErrosCatalogo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ditados_Escolas_EscolaSelecaoErrosId",
                table: "Ditados");

            migrationBuilder.DropForeignKey(
                name: "FK_RespostaSegmentoTiposErro_ErrosCatalogo_ErroCatalogoId",
                table: "RespostaSegmentoTiposErro");

            migrationBuilder.DropTable(
                name: "DitadosErrosCatalogo");

            migrationBuilder.DropIndex(
                name: "IX_RespostaSegmentoTiposErro_ErroCatalogoId",
                table: "RespostaSegmentoTiposErro");

            migrationBuilder.DropIndex(
                name: "IX_ErrosCatalogo_RegraClassificacao",
                table: "ErrosCatalogo");

            migrationBuilder.DropIndex(
                name: "IX_Ditados_EscolaSelecaoErrosId",
                table: "Ditados");

            migrationBuilder.DropColumn(
                name: "ErroCatalogoId",
                table: "RespostaSegmentoTiposErro");

            migrationBuilder.DropColumn(
                name: "RegraClassificacao",
                table: "ErrosCatalogo");

            migrationBuilder.DropColumn(
                name: "UtilizadoEm",
                table: "ErrosCatalogo");

            migrationBuilder.DropColumn(
                name: "EscolaSelecaoErrosId",
                table: "Ditados");
        }
    }
}
