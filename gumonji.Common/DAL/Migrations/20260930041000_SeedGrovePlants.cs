using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gumonji.Common.DAL.Migrations;

[DbContext(typeof(MainContext))]
[Migration("20260930041000_SeedGrovePlants")]
public sealed class SeedGrovePlants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // These were previously defined by SpawnTrees.All. Keep any harvested or
        // otherwise edited rows copied by UnifiedPlants instead of resetting them.
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "Plants"
                ("ZoneId", "Id", "CharacterId", "Type", "Subtype", "Color", "X", "Y", "Fertility", "Stage")
            VALUES
                (1, 1000, NULL, 3, 1, 8, 70, 69, 36000, 4),
                (1, 1001, NULL, 3, 2, 8, 72, 69, 100000, 4),
                (1, 1002, NULL, 3, 3, 8, 75, 75, 24000, 4),
                (1, 1003, NULL, 3, 4, 8, 75, 74, 40000, 4),
                (1, 1004, NULL, 3, 5, 8, 71, 69, 8000, 4),
                (1, 1005, NULL, 3, 1, 8, 75, 68, 36000, 4),
                (1, 1006, NULL, 3, 2, 8, 74, 74, 100000, 4),
                (1, 1007, NULL, 3, 3, 8, 68, 75, 24000, 4),
                (1, 1008, NULL, 3, 4, 8, 56, 71, 40000, 4),
                (1, 1009, NULL, 3, 5, 8, 59, 73, 8000, 4),
                (1, 1010, NULL, 3, 1, 8, 60, 68, 36000, 4),
                (1, 1011, NULL, 3, 2, 8, 60, 76, 100000, 4),
                (1, 1012, NULL, 3, 3, 8, 68, 54, 24000, 4),
                (1, 1013, NULL, 3, 4, 8, 71, 54, 40000, 4),
                (1, 1014, NULL, 3, 5, 8, 68, 52, 8000, 4),
                (1, 1015, NULL, 3, 1, 8, 71, 53, 36000, 4),
                (1, 1016, NULL, 3, 2, 8, 53, 52, 100000, 4),
                (1, 1017, NULL, 3, 3, 8, 57, 55, 24000, 4),
                (1, 1018, NULL, 3, 4, 8, 57, 57, 40000, 4),
                (1, 1019, NULL, 3, 5, 8, 53, 56, 8000, 4);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data may have changed since seeding; rolling back must not delete plants.
    }
}
