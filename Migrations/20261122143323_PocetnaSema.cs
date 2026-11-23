using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurnirSystem.Migrations
{
    /// <inheritdoc />
    public partial class PocetnaSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ekipe",
                columns: table => new
                {
                    EkipaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Naziv = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SkraceniNaziv = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mesto = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KontaktOsoba = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LogoUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ekipe", x => x.EkipaId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Korisnici",
                columns: table => new
                {
                    KorisnikId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rola = table.Column<int>(type: "int", nullable: false),
                    DatumRegistracije = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Korisnici", x => x.KorisnikId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Turniri",
                columns: table => new
                {
                    TurnirId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Naziv = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Opis = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DatumPocetka = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DatumZavrsetka = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    BrojGrupa = table.Column<int>(type: "int", nullable: false),
                    BrojEkipaPoGrupi = table.Column<int>(type: "int", nullable: false),
                    BrojKvalifikovanih = table.Column<int>(type: "int", nullable: false),
                    BodPobeda = table.Column<int>(type: "int", nullable: false),
                    BodPoraz = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    KosturPotvrdjen = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GrupnaFazaZakljucana = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turniri", x => x.TurnirId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Grupe",
                columns: table => new
                {
                    GrupaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TurnirId = table.Column<int>(type: "int", nullable: false),
                    Oznaka = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupe", x => x.GrupaId);
                    table.ForeignKey(
                        name: "FK_Grupe_Turniri_TurnirId",
                        column: x => x.TurnirId,
                        principalTable: "Turniri",
                        principalColumn: "TurnirId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TurnirEkipe",
                columns: table => new
                {
                    TurnirEkipaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TurnirId = table.Column<int>(type: "int", nullable: false),
                    EkipaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnirEkipe", x => x.TurnirEkipaId);
                    table.ForeignKey(
                        name: "FK_TurnirEkipe_Ekipe_EkipaId",
                        column: x => x.EkipaId,
                        principalTable: "Ekipe",
                        principalColumn: "EkipaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TurnirEkipe_Turniri_TurnirId",
                        column: x => x.TurnirId,
                        principalTable: "Turniri",
                        principalColumn: "TurnirId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GrupaEkipe",
                columns: table => new
                {
                    GrupaEkipaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GrupaId = table.Column<int>(type: "int", nullable: false),
                    EkipaId = table.Column<int>(type: "int", nullable: false),
                    RucniPlasman = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrupaEkipe", x => x.GrupaEkipaId);
                    table.ForeignKey(
                        name: "FK_GrupaEkipe_Ekipe_EkipaId",
                        column: x => x.EkipaId,
                        principalTable: "Ekipe",
                        principalColumn: "EkipaId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GrupaEkipe_Grupe_GrupaId",
                        column: x => x.GrupaId,
                        principalTable: "Grupe",
                        principalColumn: "GrupaId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Mecevi",
                columns: table => new
                {
                    MecId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TurnirId = table.Column<int>(type: "int", nullable: false),
                    GrupaId = table.Column<int>(type: "int", nullable: true),
                    Ekipa1Id = table.Column<int>(type: "int", nullable: false),
                    Ekipa2Id = table.Column<int>(type: "int", nullable: false),
                    RezultatEkipa1 = table.Column<int>(type: "int", nullable: true),
                    RezultatEkipa2 = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Faza = table.Column<int>(type: "int", nullable: false),
                    DatumVreme = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mecevi", x => x.MecId);
                    table.ForeignKey(
                        name: "FK_Mecevi_Ekipe_Ekipa1Id",
                        column: x => x.Ekipa1Id,
                        principalTable: "Ekipe",
                        principalColumn: "EkipaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mecevi_Ekipe_Ekipa2Id",
                        column: x => x.Ekipa2Id,
                        principalTable: "Ekipe",
                        principalColumn: "EkipaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mecevi_Grupe_GrupaId",
                        column: x => x.GrupaId,
                        principalTable: "Grupe",
                        principalColumn: "GrupaId");
                    table.ForeignKey(
                        name: "FK_Mecevi_Turniri_TurnirId",
                        column: x => x.TurnirId,
                        principalTable: "Turniri",
                        principalColumn: "TurnirId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EliminacionaKola",
                columns: table => new
                {
                    EliminacionoKoloId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TurnirId = table.Column<int>(type: "int", nullable: false),
                    BrojKola = table.Column<int>(type: "int", nullable: false),
                    Pozicija = table.Column<int>(type: "int", nullable: false),
                    JeBye = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Ekipa1Id = table.Column<int>(type: "int", nullable: true),
                    Ekipa2Id = table.Column<int>(type: "int", nullable: true),
                    PobednikId = table.Column<int>(type: "int", nullable: true),
                    MecId = table.Column<int>(type: "int", nullable: true),
                    RezultatEkipa1 = table.Column<int>(type: "int", nullable: true),
                    RezultatEkipa2 = table.Column<int>(type: "int", nullable: true),
                    DatumVreme = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Odlozen = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EliminacionaKola", x => x.EliminacionoKoloId);
                    table.ForeignKey(
                        name: "FK_EliminacionaKola_Ekipe_Ekipa1Id",
                        column: x => x.Ekipa1Id,
                        principalTable: "Ekipe",
                        principalColumn: "EkipaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EliminacionaKola_Ekipe_Ekipa2Id",
                        column: x => x.Ekipa2Id,
                        principalTable: "Ekipe",
                        principalColumn: "EkipaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EliminacionaKola_Ekipe_PobednikId",
                        column: x => x.PobednikId,
                        principalTable: "Ekipe",
                        principalColumn: "EkipaId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EliminacionaKola_Mecevi_MecId",
                        column: x => x.MecId,
                        principalTable: "Mecevi",
                        principalColumn: "MecId");
                    table.ForeignKey(
                        name: "FK_EliminacionaKola_Turniri_TurnirId",
                        column: x => x.TurnirId,
                        principalTable: "Turniri",
                        principalColumn: "TurnirId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EliminacionaKola_Ekipa1Id",
                table: "EliminacionaKola",
                column: "Ekipa1Id");

            migrationBuilder.CreateIndex(
                name: "IX_EliminacionaKola_Ekipa2Id",
                table: "EliminacionaKola",
                column: "Ekipa2Id");

            migrationBuilder.CreateIndex(
                name: "IX_EliminacionaKola_MecId",
                table: "EliminacionaKola",
                column: "MecId");

            migrationBuilder.CreateIndex(
                name: "IX_EliminacionaKola_PobednikId",
                table: "EliminacionaKola",
                column: "PobednikId");

            migrationBuilder.CreateIndex(
                name: "IX_EliminacionaKola_TurnirId",
                table: "EliminacionaKola",
                column: "TurnirId");

            migrationBuilder.CreateIndex(
                name: "IX_GrupaEkipe_EkipaId",
                table: "GrupaEkipe",
                column: "EkipaId");

            migrationBuilder.CreateIndex(
                name: "IX_GrupaEkipe_GrupaId_EkipaId",
                table: "GrupaEkipe",
                columns: new[] { "GrupaId", "EkipaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grupe_TurnirId",
                table: "Grupe",
                column: "TurnirId");

            migrationBuilder.CreateIndex(
                name: "IX_Mecevi_Ekipa1Id",
                table: "Mecevi",
                column: "Ekipa1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Mecevi_Ekipa2Id",
                table: "Mecevi",
                column: "Ekipa2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Mecevi_GrupaId",
                table: "Mecevi",
                column: "GrupaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mecevi_TurnirId",
                table: "Mecevi",
                column: "TurnirId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnirEkipe_EkipaId",
                table: "TurnirEkipe",
                column: "EkipaId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnirEkipe_TurnirId_EkipaId",
                table: "TurnirEkipe",
                columns: new[] { "TurnirId", "EkipaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EliminacionaKola");

            migrationBuilder.DropTable(
                name: "GrupaEkipe");

            migrationBuilder.DropTable(
                name: "Korisnici");

            migrationBuilder.DropTable(
                name: "TurnirEkipe");

            migrationBuilder.DropTable(
                name: "Mecevi");

            migrationBuilder.DropTable(
                name: "Ekipe");

            migrationBuilder.DropTable(
                name: "Grupe");

            migrationBuilder.DropTable(
                name: "Turniri");
        }
    }
}
