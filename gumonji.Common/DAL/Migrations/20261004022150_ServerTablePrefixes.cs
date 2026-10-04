using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gumonji.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ServerTablePrefixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Startup may already have consumed the legacy staging table.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "BackdCharacterLegacy" (
                    UserId INTEGER NOT NULL PRIMARY KEY,
                    Payload BLOB NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                CREATE TABLE "backd.CharacterLegacy" (
                    UserId INTEGER NOT NULL PRIMARY KEY,
                    Payload BLOB NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                INSERT INTO "backd.CharacterLegacy" SELECT UserId, Payload, UpdatedAt FROM "BackdCharacterLegacy";
                DROP TABLE "BackdCharacterLegacy";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_BackdCharacterEquipment_BackdCharacters_UserId",
                table: "BackdCharacterEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_BackdCharacterExperience_BackdCharacters_UserId",
                table: "BackdCharacterExperience");

            migrationBuilder.DropForeignKey(
                name: "FK_BackdCharacterExtension_BackdCharacters_UserId",
                table: "BackdCharacterExtension");

            migrationBuilder.DropForeignKey(
                name: "FK_BackdCharacterItem_BackdCharacters_UserId",
                table: "BackdCharacterItem");

            migrationBuilder.DropForeignKey(
                name: "FK_BackdCharacterItemComment_BackdCharacterItem_UserId_Slot",
                table: "BackdCharacterItemComment");

            migrationBuilder.DropForeignKey(
                name: "FK_BackdCharacterItemParameter_BackdCharacterItem_UserId_Slot",
                table: "BackdCharacterItemParameter");

            migrationBuilder.DropForeignKey(
                name: "FK_BackdCharacterParameter_BackdCharacters_UserId",
                table: "BackdCharacterParameter");

            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Users_UserId",
                table: "Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Characters_CharacterId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Plants_Characters_CharacterId",
                table: "Plants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Plants",
                table: "Plants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Characters",
                table: "Characters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdSequences",
                table: "BackdSequences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdHistories",
                table: "BackdHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdCharacters",
                table: "BackdCharacters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdCharacterParameter",
                table: "BackdCharacterParameter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdCharacterItemParameter",
                table: "BackdCharacterItemParameter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdCharacterItemComment",
                table: "BackdCharacterItemComment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdCharacterItem",
                table: "BackdCharacterItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdCharacterExtension",
                table: "BackdCharacterExtension");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdCharacterExperience",
                table: "BackdCharacterExperience");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BackdCharacterEquipment",
                table: "BackdCharacterEquipment");

            migrationBuilder.RenameTable(
                name: "Plants",
                newName: "zone.Plants");

            migrationBuilder.RenameTable(
                name: "InventoryItems",
                newName: "zone.InventoryItems");

            migrationBuilder.RenameTable(
                name: "Characters",
                newName: "zone.Characters");

            migrationBuilder.RenameTable(
                name: "BackdSequences",
                newName: "backd.Sequences");

            migrationBuilder.RenameTable(
                name: "BackdHistories",
                newName: "backd.Histories");

            migrationBuilder.RenameTable(
                name: "BackdCharacters",
                newName: "backd.Characters");

            migrationBuilder.RenameTable(
                name: "BackdCharacterParameter",
                newName: "backd.CharacterParameters");

            migrationBuilder.RenameTable(
                name: "BackdCharacterItemParameter",
                newName: "backd.CharacterItemParameters");

            migrationBuilder.RenameTable(
                name: "BackdCharacterItemComment",
                newName: "backd.CharacterItemComments");

            migrationBuilder.RenameTable(
                name: "BackdCharacterItem",
                newName: "backd.CharacterItems");

            migrationBuilder.RenameTable(
                name: "BackdCharacterExtension",
                newName: "backd.CharacterExtensions");

            migrationBuilder.RenameTable(
                name: "BackdCharacterExperience",
                newName: "backd.CharacterExperiences");

            migrationBuilder.RenameTable(
                name: "BackdCharacterEquipment",
                newName: "backd.CharacterEquipment");

            migrationBuilder.RenameIndex(
                name: "IX_Plants_ZoneId_X_Y",
                table: "zone.Plants",
                newName: "IX_zone.Plants_ZoneId_X_Y");

            migrationBuilder.RenameIndex(
                name: "IX_Plants_CharacterId",
                table: "zone.Plants",
                newName: "IX_zone.Plants_CharacterId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryItems_CharacterId_Slot",
                table: "zone.InventoryItems",
                newName: "IX_zone.InventoryItems_CharacterId_Slot");

            migrationBuilder.RenameIndex(
                name: "IX_Characters_UserId",
                table: "zone.Characters",
                newName: "IX_zone.Characters_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_zone.Plants",
                table: "zone.Plants",
                columns: new[] { "ZoneId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_zone.InventoryItems",
                table: "zone.InventoryItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_zone.Characters",
                table: "zone.Characters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.Sequences",
                table: "backd.Sequences",
                column: "Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.Histories",
                table: "backd.Histories",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.Characters",
                table: "backd.Characters",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.CharacterParameters",
                table: "backd.CharacterParameters",
                columns: new[] { "UserId", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.CharacterItemParameters",
                table: "backd.CharacterItemParameters",
                columns: new[] { "UserId", "Slot", "Secret", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.CharacterItemComments",
                table: "backd.CharacterItemComments",
                columns: new[] { "UserId", "Slot", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.CharacterItems",
                table: "backd.CharacterItems",
                columns: new[] { "UserId", "Slot" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.CharacterExtensions",
                table: "backd.CharacterExtensions",
                columns: new[] { "UserId", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.CharacterExperiences",
                table: "backd.CharacterExperiences",
                columns: new[] { "UserId", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_backd.CharacterEquipment",
                table: "backd.CharacterEquipment",
                columns: new[] { "UserId", "Slot" });

            migrationBuilder.AddForeignKey(
                name: "FK_backd.CharacterEquipment_backd.Characters_UserId",
                table: "backd.CharacterEquipment",
                column: "UserId",
                principalTable: "backd.Characters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_backd.CharacterExperiences_backd.Characters_UserId",
                table: "backd.CharacterExperiences",
                column: "UserId",
                principalTable: "backd.Characters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_backd.CharacterExtensions_backd.Characters_UserId",
                table: "backd.CharacterExtensions",
                column: "UserId",
                principalTable: "backd.Characters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_backd.CharacterItemComments_backd.CharacterItems_UserId_Slot",
                table: "backd.CharacterItemComments",
                columns: new[] { "UserId", "Slot" },
                principalTable: "backd.CharacterItems",
                principalColumns: new[] { "UserId", "Slot" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_backd.CharacterItemParameters_backd.CharacterItems_UserId_Slot",
                table: "backd.CharacterItemParameters",
                columns: new[] { "UserId", "Slot" },
                principalTable: "backd.CharacterItems",
                principalColumns: new[] { "UserId", "Slot" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_backd.CharacterItems_backd.Characters_UserId",
                table: "backd.CharacterItems",
                column: "UserId",
                principalTable: "backd.Characters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_backd.CharacterParameters_backd.Characters_UserId",
                table: "backd.CharacterParameters",
                column: "UserId",
                principalTable: "backd.Characters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_zone.Characters_Users_UserId",
                table: "zone.Characters",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_zone.InventoryItems_zone.Characters_CharacterId",
                table: "zone.InventoryItems",
                column: "CharacterId",
                principalTable: "zone.Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_zone.Plants_zone.Characters_CharacterId",
                table: "zone.Plants",
                column: "CharacterId",
                principalTable: "zone.Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Startup may already have consumed the legacy staging table.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "backd.CharacterLegacy" (
                    UserId INTEGER NOT NULL PRIMARY KEY,
                    Payload BLOB NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                CREATE TABLE "BackdCharacterLegacy" (
                    UserId INTEGER NOT NULL PRIMARY KEY,
                    Payload BLOB NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                INSERT INTO "BackdCharacterLegacy" SELECT UserId, Payload, UpdatedAt FROM "backd.CharacterLegacy";
                DROP TABLE "backd.CharacterLegacy";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_backd.CharacterEquipment_backd.Characters_UserId",
                table: "backd.CharacterEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_backd.CharacterExperiences_backd.Characters_UserId",
                table: "backd.CharacterExperiences");

            migrationBuilder.DropForeignKey(
                name: "FK_backd.CharacterExtensions_backd.Characters_UserId",
                table: "backd.CharacterExtensions");

            migrationBuilder.DropForeignKey(
                name: "FK_backd.CharacterItemComments_backd.CharacterItems_UserId_Slot",
                table: "backd.CharacterItemComments");

            migrationBuilder.DropForeignKey(
                name: "FK_backd.CharacterItemParameters_backd.CharacterItems_UserId_Slot",
                table: "backd.CharacterItemParameters");

            migrationBuilder.DropForeignKey(
                name: "FK_backd.CharacterItems_backd.Characters_UserId",
                table: "backd.CharacterItems");

            migrationBuilder.DropForeignKey(
                name: "FK_backd.CharacterParameters_backd.Characters_UserId",
                table: "backd.CharacterParameters");

            migrationBuilder.DropForeignKey(
                name: "FK_zone.Characters_Users_UserId",
                table: "zone.Characters");

            migrationBuilder.DropForeignKey(
                name: "FK_zone.InventoryItems_zone.Characters_CharacterId",
                table: "zone.InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_zone.Plants_zone.Characters_CharacterId",
                table: "zone.Plants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_zone.Plants",
                table: "zone.Plants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_zone.InventoryItems",
                table: "zone.InventoryItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_zone.Characters",
                table: "zone.Characters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.Sequences",
                table: "backd.Sequences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.Histories",
                table: "backd.Histories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.Characters",
                table: "backd.Characters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.CharacterParameters",
                table: "backd.CharacterParameters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.CharacterItems",
                table: "backd.CharacterItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.CharacterItemParameters",
                table: "backd.CharacterItemParameters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.CharacterItemComments",
                table: "backd.CharacterItemComments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.CharacterExtensions",
                table: "backd.CharacterExtensions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.CharacterExperiences",
                table: "backd.CharacterExperiences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_backd.CharacterEquipment",
                table: "backd.CharacterEquipment");

            migrationBuilder.RenameTable(
                name: "zone.Plants",
                newName: "Plants");

            migrationBuilder.RenameTable(
                name: "zone.InventoryItems",
                newName: "InventoryItems");

            migrationBuilder.RenameTable(
                name: "zone.Characters",
                newName: "Characters");

            migrationBuilder.RenameTable(
                name: "backd.Sequences",
                newName: "BackdSequences");

            migrationBuilder.RenameTable(
                name: "backd.Histories",
                newName: "BackdHistories");

            migrationBuilder.RenameTable(
                name: "backd.Characters",
                newName: "BackdCharacters");

            migrationBuilder.RenameTable(
                name: "backd.CharacterParameters",
                newName: "BackdCharacterParameter");

            migrationBuilder.RenameTable(
                name: "backd.CharacterItems",
                newName: "BackdCharacterItem");

            migrationBuilder.RenameTable(
                name: "backd.CharacterItemParameters",
                newName: "BackdCharacterItemParameter");

            migrationBuilder.RenameTable(
                name: "backd.CharacterItemComments",
                newName: "BackdCharacterItemComment");

            migrationBuilder.RenameTable(
                name: "backd.CharacterExtensions",
                newName: "BackdCharacterExtension");

            migrationBuilder.RenameTable(
                name: "backd.CharacterExperiences",
                newName: "BackdCharacterExperience");

            migrationBuilder.RenameTable(
                name: "backd.CharacterEquipment",
                newName: "BackdCharacterEquipment");

            migrationBuilder.RenameIndex(
                name: "IX_zone.Plants_ZoneId_X_Y",
                table: "Plants",
                newName: "IX_Plants_ZoneId_X_Y");

            migrationBuilder.RenameIndex(
                name: "IX_zone.Plants_CharacterId",
                table: "Plants",
                newName: "IX_Plants_CharacterId");

            migrationBuilder.RenameIndex(
                name: "IX_zone.InventoryItems_CharacterId_Slot",
                table: "InventoryItems",
                newName: "IX_InventoryItems_CharacterId_Slot");

            migrationBuilder.RenameIndex(
                name: "IX_zone.Characters_UserId",
                table: "Characters",
                newName: "IX_Characters_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Plants",
                table: "Plants",
                columns: new[] { "ZoneId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryItems",
                table: "InventoryItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Characters",
                table: "Characters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdSequences",
                table: "BackdSequences",
                column: "Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdHistories",
                table: "BackdHistories",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdCharacters",
                table: "BackdCharacters",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdCharacterParameter",
                table: "BackdCharacterParameter",
                columns: new[] { "UserId", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdCharacterItem",
                table: "BackdCharacterItem",
                columns: new[] { "UserId", "Slot" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdCharacterItemParameter",
                table: "BackdCharacterItemParameter",
                columns: new[] { "UserId", "Slot", "Secret", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdCharacterItemComment",
                table: "BackdCharacterItemComment",
                columns: new[] { "UserId", "Slot", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdCharacterExtension",
                table: "BackdCharacterExtension",
                columns: new[] { "UserId", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdCharacterExperience",
                table: "BackdCharacterExperience",
                columns: new[] { "UserId", "Index" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BackdCharacterEquipment",
                table: "BackdCharacterEquipment",
                columns: new[] { "UserId", "Slot" });

            migrationBuilder.AddForeignKey(
                name: "FK_BackdCharacterEquipment_BackdCharacters_UserId",
                table: "BackdCharacterEquipment",
                column: "UserId",
                principalTable: "BackdCharacters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BackdCharacterExperience_BackdCharacters_UserId",
                table: "BackdCharacterExperience",
                column: "UserId",
                principalTable: "BackdCharacters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BackdCharacterExtension_BackdCharacters_UserId",
                table: "BackdCharacterExtension",
                column: "UserId",
                principalTable: "BackdCharacters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BackdCharacterItem_BackdCharacters_UserId",
                table: "BackdCharacterItem",
                column: "UserId",
                principalTable: "BackdCharacters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BackdCharacterItemComment_BackdCharacterItem_UserId_Slot",
                table: "BackdCharacterItemComment",
                columns: new[] { "UserId", "Slot" },
                principalTable: "BackdCharacterItem",
                principalColumns: new[] { "UserId", "Slot" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BackdCharacterItemParameter_BackdCharacterItem_UserId_Slot",
                table: "BackdCharacterItemParameter",
                columns: new[] { "UserId", "Slot" },
                principalTable: "BackdCharacterItem",
                principalColumns: new[] { "UserId", "Slot" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BackdCharacterParameter_BackdCharacters_UserId",
                table: "BackdCharacterParameter",
                column: "UserId",
                principalTable: "BackdCharacters",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Users_UserId",
                table: "Characters",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Characters_CharacterId",
                table: "InventoryItems",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_Characters_CharacterId",
                table: "Plants",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
