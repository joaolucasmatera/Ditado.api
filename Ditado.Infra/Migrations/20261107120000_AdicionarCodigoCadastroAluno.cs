using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ditado.Infra.Migrations;

public partial class AdicionarCodigoCadastroAluno : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "CodigoCadastro", table: "Usuarios", type: "varchar(32)", maxLength: 32, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Usuarios_CodigoCadastro", table: "Usuarios", column: "CodigoCadastro", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Usuarios_CodigoCadastro", table: "Usuarios");
        migrationBuilder.DropColumn(name: "CodigoCadastro", table: "Usuarios");
    }
}
