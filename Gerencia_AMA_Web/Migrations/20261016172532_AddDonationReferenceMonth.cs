using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerencia_AMA_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationReferenceMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MesReferencia",
                table: "DebitosMembros",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DebitosMembros_UsuarioId_TipoDoacaoId_MesReferencia",
                table: "DebitosMembros",
                columns: new[] { "UsuarioId", "TipoDoacaoId", "MesReferencia" },
                unique: true,
                filter: "MesReferencia IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DebitosMembros_UsuarioId_TipoDoacaoId_MesReferencia",
                table: "DebitosMembros");

            migrationBuilder.DropColumn(
                name: "MesReferencia",
                table: "DebitosMembros");

        }
    }
}
