using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations
{
    /// <inheritdoc />
    public partial class SuportarMultiplosTiposErro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RespostaSegmentoTiposErro",
                columns: table => new
                {
                    RespostaSegmentoId = table.Column<int>(type: "int", nullable: false),
                    TipoErro = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostaSegmentoTiposErro", x => new { x.RespostaSegmentoId, x.TipoErro });
                    table.ForeignKey(
                        name: "FK_RespostaSegmentoTiposErro_RespostaSegmentos_RespostaSegmento~",
                        column: x => x.RespostaSegmentoId,
                        principalTable: "RespostaSegmentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RespostaSegmentoTiposErro");
        }
    }
}
