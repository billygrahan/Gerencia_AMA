using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerencia_AMA_Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialTesoureiroAndRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comprovantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UrlCloudinary = table.Column<string>(type: "TEXT", nullable: false),
                    PublicIdCloudinary = table.Column<string>(type: "TEXT", nullable: false),
                    DataEnvio = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprovantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposDoacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Data_Expiracao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDoacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Senha = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Perfil = table.Column<int>(type: "INTEGER", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DebitosMembros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoDoacaoId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Valor = table.Column<double>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ComprovanteId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebitosMembros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebitosMembros_Comprovantes_ComprovanteId",
                        column: x => x.ComprovanteId,
                        principalTable: "Comprovantes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DebitosMembros_TiposDoacao_TipoDoacaoId",
                        column: x => x.TipoDoacaoId,
                        principalTable: "TiposDoacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DebitosMembros_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Email", "Nome", "Perfil", "RefreshToken", "RefreshTokenExpiryTime", "Senha" },
                values: new object[] { 1, "tesourariaiasdgenibau@gmail.com", "Billy Grahan", 2, null, null, "$2a$11$N7I1fPjZ.x/k4A2/6i4gO.8xW/V7lB7w3KjX7xZ/8m2K2d0f0G1e2" });

            migrationBuilder.CreateIndex(
                name: "IX_DebitosMembros_ComprovanteId",
                table: "DebitosMembros",
                column: "ComprovanteId");

            migrationBuilder.CreateIndex(
                name: "IX_DebitosMembros_TipoDoacaoId",
                table: "DebitosMembros",
                column: "TipoDoacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_DebitosMembros_UsuarioId",
                table: "DebitosMembros",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DebitosMembros");

            migrationBuilder.DropTable(
                name: "Comprovantes");

            migrationBuilder.DropTable(
                name: "TiposDoacao");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
