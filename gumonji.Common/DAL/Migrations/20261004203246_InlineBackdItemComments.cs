using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gumonji.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InlineBackdItemComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<byte[]>(
                name: "Comment0",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Comment1",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Comment2",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Comment3",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Comment4",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Comment5",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Comment6",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Comment7",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true);
            // Move bytes into the fixed protocol fields before removing the child table.
            migrationBuilder.Sql("""
                UPDATE "backd.CharacterItems" SET
                    "Comment0" = (SELECT "Text" FROM "backd.CharacterItemComments" c WHERE c."UserId" = "backd.CharacterItems"."UserId" AND c."Slot" = "backd.CharacterItems"."Slot" AND c."Index" = 0),
                    "Comment1" = (SELECT "Text" FROM "backd.CharacterItemComments" c WHERE c."UserId" = "backd.CharacterItems"."UserId" AND c."Slot" = "backd.CharacterItems"."Slot" AND c."Index" = 1),
                    "Comment2" = (SELECT "Text" FROM "backd.CharacterItemComments" c WHERE c."UserId" = "backd.CharacterItems"."UserId" AND c."Slot" = "backd.CharacterItems"."Slot" AND c."Index" = 2),
                    "Comment3" = (SELECT "Text" FROM "backd.CharacterItemComments" c WHERE c."UserId" = "backd.CharacterItems"."UserId" AND c."Slot" = "backd.CharacterItems"."Slot" AND c."Index" = 3),
                    "Comment4" = (SELECT "Text" FROM "backd.CharacterItemComments" c WHERE c."UserId" = "backd.CharacterItems"."UserId" AND c."Slot" = "backd.CharacterItems"."Slot" AND c."Index" = 4),
                    "Comment5" = (SELECT "Text" FROM "backd.CharacterItemComments" c WHERE c."UserId" = "backd.CharacterItems"."UserId" AND c."Slot" = "backd.CharacterItems"."Slot" AND c."Index" = 5),
                    "Comment6" = (SELECT "Text" FROM "backd.CharacterItemComments" c WHERE c."UserId" = "backd.CharacterItems"."UserId" AND c."Slot" = "backd.CharacterItems"."Slot" AND c."Index" = 6),
                    "Comment7" = (SELECT "Text" FROM "backd.CharacterItemComments" c WHERE c."UserId" = "backd.CharacterItems"."UserId" AND c."Slot" = "backd.CharacterItems"."Slot" AND c."Index" = 7);
                """);
            migrationBuilder.DropTable(
                name: "backd.CharacterItemComments");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "backd.CharacterItemComments",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backd.CharacterItemComments", x => new { x.UserId, x.Slot, x.Index });
                    table.ForeignKey(
                        name: "FK_backd.CharacterItemComments_backd.CharacterItems_UserId_Slot",
                        columns: x => new { x.UserId, x.Slot },
                        principalTable: "backd.CharacterItems",
                        principalColumns: new[] { "UserId", "Slot" },
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.Sql("""
                INSERT INTO "backd.CharacterItemComments" ("UserId", "Slot", "Index", "Text") SELECT "UserId", "Slot", 0, "Comment0" FROM "backd.CharacterItems" WHERE "Comment0" IS NOT NULL;
                INSERT INTO "backd.CharacterItemComments" ("UserId", "Slot", "Index", "Text") SELECT "UserId", "Slot", 1, "Comment1" FROM "backd.CharacterItems" WHERE "Comment1" IS NOT NULL;
                INSERT INTO "backd.CharacterItemComments" ("UserId", "Slot", "Index", "Text") SELECT "UserId", "Slot", 2, "Comment2" FROM "backd.CharacterItems" WHERE "Comment2" IS NOT NULL;
                INSERT INTO "backd.CharacterItemComments" ("UserId", "Slot", "Index", "Text") SELECT "UserId", "Slot", 3, "Comment3" FROM "backd.CharacterItems" WHERE "Comment3" IS NOT NULL;
                INSERT INTO "backd.CharacterItemComments" ("UserId", "Slot", "Index", "Text") SELECT "UserId", "Slot", 4, "Comment4" FROM "backd.CharacterItems" WHERE "Comment4" IS NOT NULL;
                INSERT INTO "backd.CharacterItemComments" ("UserId", "Slot", "Index", "Text") SELECT "UserId", "Slot", 5, "Comment5" FROM "backd.CharacterItems" WHERE "Comment5" IS NOT NULL;
                INSERT INTO "backd.CharacterItemComments" ("UserId", "Slot", "Index", "Text") SELECT "UserId", "Slot", 6, "Comment6" FROM "backd.CharacterItems" WHERE "Comment6" IS NOT NULL;
                INSERT INTO "backd.CharacterItemComments" ("UserId", "Slot", "Index", "Text") SELECT "UserId", "Slot", 7, "Comment7" FROM "backd.CharacterItems" WHERE "Comment7" IS NOT NULL;
                """);
            migrationBuilder.DropColumn(
                name: "Comment0",
                table: "backd.CharacterItems");

            migrationBuilder.DropColumn(
                name: "Comment1",
                table: "backd.CharacterItems");

            migrationBuilder.DropColumn(
                name: "Comment2",
                table: "backd.CharacterItems");

            migrationBuilder.DropColumn(
                name: "Comment3",
                table: "backd.CharacterItems");

            migrationBuilder.DropColumn(
                name: "Comment4",
                table: "backd.CharacterItems");

            migrationBuilder.DropColumn(
                name: "Comment5",
                table: "backd.CharacterItems");

            migrationBuilder.DropColumn(
                name: "Comment6",
                table: "backd.CharacterItems");

            migrationBuilder.DropColumn(
                name: "Comment7",
                table: "backd.CharacterItems");

        }
    }
}
