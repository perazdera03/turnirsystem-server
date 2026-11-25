using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurnirSystem.Migrations
{
    /// <inheritdoc />
    public partial class brisanjenekoriscenihpolja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EliminacionaKola_Mecevi_MecId",
                table: "EliminacionaKola");

            migrationBuilder.DropIndex(
                name: "IX_EliminacionaKola_MecId",
                table: "EliminacionaKola");

            migrationBuilder.DropColumn(
                name: "DatumRegistracije",
                table: "Korisnici");

            migrationBuilder.DropColumn(
                name: "MecId",
                table: "EliminacionaKola");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DatumRegistracije",
                table: "Korisnici",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "MecId",
                table: "EliminacionaKola",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EliminacionaKola_MecId",
                table: "EliminacionaKola",
                column: "MecId");

            migrationBuilder.AddForeignKey(
                name: "FK_EliminacionaKola_Mecevi_MecId",
                table: "EliminacionaKola",
                column: "MecId",
                principalTable: "Mecevi",
                principalColumn: "MecId");
        }
    }
}
