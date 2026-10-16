using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerencia_AMA_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationTypeOpeningMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MesAbertura",
                table: "TiposDoacao",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MesAbertura",
                table: "TiposDoacao");
        }
    }
}
