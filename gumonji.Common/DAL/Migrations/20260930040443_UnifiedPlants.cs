using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gumonji.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedPlants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Subtype = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    Fertility = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => new { x.ZoneId, x.Id });
                    table.ForeignKey(
                        name: "FK_Plants_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Plants_CharacterId",
                table: "Plants",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Plants_ZoneId_X_Y",
                table: "Plants",
                columns: new[] { "ZoneId", "X", "Y" },
                unique: true);

            migrationBuilder.Sql("""
                WITH grove(Id, Subtype, X, Y) AS (VALUES
                    (1000, 1, 70, 69), (1001, 2, 72, 69), (1002, 3, 75, 75),
                    (1003, 4, 75, 74), (1004, 5, 71, 69), (1005, 1, 75, 68),
                    (1006, 2, 74, 74), (1007, 3, 68, 75), (1008, 4, 56, 71),
                    (1009, 5, 59, 73), (1010, 1, 60, 68), (1011, 2, 60, 76),
                    (1012, 3, 68, 54), (1013, 4, 71, 54), (1014, 5, 68, 52),
                    (1015, 1, 71, 53), (1016, 2, 53, 52), (1017, 3, 57, 55),
                    (1018, 4, 57, 57), (1019, 5, 53, 56))
                INSERT INTO "Plants" ("ZoneId", "Id", "CharacterId", "Type", "Subtype", "Color", "X", "Y", "Fertility", "Stage")
                SELECT ps."ZoneId", ps."PlantId", NULL, 3, grove.Subtype, 8, grove.X, grove.Y,
                       ps."Fertility", ps."Stage"
                FROM "PlantStates" AS ps JOIN grove ON grove.Id = ps."PlantId";
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Plants" ("ZoneId", "Id", "CharacterId", "Type", "Subtype", "Color", "X", "Y", "Fertility", "Stage")
                SELECT "ZoneId", 100000 + "Id", "CharacterId", 3, "Subtype", "Color", "X", "Y", "Fertility", "Stage"
                FROM "PlantedSeeds";
                """);

            migrationBuilder.DropTable(name: "PlantedSeeds");
            migrationBuilder.DropTable(name: "PlantStates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantedSeeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: false),
                    Fertility = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    Subtype = table.Column<int>(type: "INTEGER", nullable: false),
                    X = table.Column<int>(type: "INTEGER", nullable: false),
                    Y = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantedSeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantedSeeds_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlantStates",
                columns: table => new
                {
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlantId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fertility = table.Column<int>(type: "INTEGER", nullable: false),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantStates", x => new { x.ZoneId, x.PlantId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantedSeeds_CharacterId",
                table: "PlantedSeeds",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantedSeeds_ZoneId_X_Y",
                table: "PlantedSeeds",
                columns: new[] { "ZoneId", "X", "Y" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "PlantStates" ("ZoneId", "PlantId", "Fertility", "Stage")
                SELECT "ZoneId", "Id", "Fertility", "Stage" FROM "Plants" WHERE "Id" < 100000;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "PlantedSeeds" ("Id", "ZoneId", "CharacterId", "X", "Y", "Subtype", "Color", "Fertility", "Stage")
                SELECT "Id" - 100000, "ZoneId", "CharacterId", "X", "Y", "Subtype", "Color", "Fertility", "Stage"
                FROM "Plants" WHERE "Id" >= 100000 AND "CharacterId" IS NOT NULL;
                """);

            migrationBuilder.DropTable(name: "Plants");
        }
    }
}
