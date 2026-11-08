using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarEscolaTurma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EscolaId",
                table: "Turmas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_EscolaId",
                table: "Turmas",
                column: "EscolaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Turmas_Escolas_EscolaId",
                table: "Turmas",
                column: "EscolaId",
                principalTable: "Escolas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turmas_Escolas_EscolaId",
                table: "Turmas");

            migrationBuilder.DropIndex(
                name: "IX_Turmas_EscolaId",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "EscolaId",
                table: "Turmas");
        }
    }
}
