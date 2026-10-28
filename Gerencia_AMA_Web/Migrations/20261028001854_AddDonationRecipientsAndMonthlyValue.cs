using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerencia_AMA_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationRecipientsAndMonthlyValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TodosUsuarios",
                table: "TiposDoacao",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPadrao",
                table: "TiposDoacao",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TipoDoacaoUsuarios",
                columns: table => new
                {
                    TipoDoacaoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoDoacaoUsuarios", x => new { x.TipoDoacaoId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_TipoDoacaoUsuarios_TiposDoacao_TipoDoacaoId",
                        column: x => x.TipoDoacaoId,
                        principalTable: "TiposDoacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TipoDoacaoUsuarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TipoDoacaoUsuarios_UsuarioId",
                table: "TipoDoacaoUsuarios",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TipoDoacaoUsuarios");

            migrationBuilder.DropColumn(
                name: "TodosUsuarios",
                table: "TiposDoacao");

            migrationBuilder.DropColumn(
                name: "ValorPadrao",
                table: "TiposDoacao");
        }
    }
}
