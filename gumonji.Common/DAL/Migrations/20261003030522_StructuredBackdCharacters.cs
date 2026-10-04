using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gumonji.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class StructuredBackdCharacters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the source until the startup byte-codec conversion commits successfully.
            migrationBuilder.RenameTable(name: "BackdCharacters", newName: "BackdCharacterLegacy");
            migrationBuilder.CreateTable(
                name: "BackdCharacters",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<System.DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_BackdCharacters", x => x.UserId));

            migrationBuilder.AddColumn<uint>(
                name: "ClockCount",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ColorType",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Consumption",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "CreatedUnixTime",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EditLevel",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EyeType",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "FavoriteColor",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "LastGumolotTime",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "LastLoginTime",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastSaveGameTime",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoginCount",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "LoginTotalSeconds",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "MarkName",
                table: "BackdCharacters",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MoveCount",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Nickname",
                table: "BackdCharacters",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Novice",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "OwnerUserId",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PortalServer",
                table: "BackdCharacters",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Subtype",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalExchangedCount",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalExchangedMoney",
                table: "BackdCharacters",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BackdCharacterEquipment",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackdCharacterEquipment", x => new { x.UserId, x.Slot });
                    table.ForeignKey(
                        name: "FK_BackdCharacterEquipment_BackdCharacters_UserId",
                        column: x => x.UserId,
                        principalTable: "BackdCharacters",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackdCharacterExperience",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<byte>(type: "INTEGER", nullable: false),
                    Type = table.Column<byte>(type: "INTEGER", nullable: false),
                    Subtype = table.Column<byte>(type: "INTEGER", nullable: false),
                    Color = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackdCharacterExperience", x => new { x.UserId, x.Index });
                    table.ForeignKey(
                        name: "FK_BackdCharacterExperience_BackdCharacters_UserId",
                        column: x => x.UserId,
                        principalTable: "BackdCharacters",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackdCharacterExtension",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Line = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackdCharacterExtension", x => new { x.UserId, x.Index });
                    table.ForeignKey(
                        name: "FK_BackdCharacterExtension_BackdCharacters_UserId",
                        column: x => x.UserId,
                        principalTable: "BackdCharacters",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackdCharacterItem",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeName = table.Column<string>(type: "TEXT", nullable: false),
                    Subtype = table.Column<int>(type: "INTEGER", nullable: true),
                    ColorType = table.Column<int>(type: "INTEGER", nullable: true),
                    CreateId = table.Column<byte[]>(type: "BLOB", nullable: true),
                    StickerUserId = table.Column<uint>(type: "INTEGER", nullable: true),
                    FamilyName = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CreatedGameTime = table.Column<long>(type: "INTEGER", nullable: true),
                    Generation = table.Column<uint>(type: "INTEGER", nullable: true),
                    Fertilizer = table.Column<uint>(type: "INTEGER", nullable: true),
                    Water = table.Column<uint>(type: "INTEGER", nullable: true),
                    C6H4 = table.Column<uint>(type: "INTEGER", nullable: true),
                    SiO2 = table.Column<uint>(type: "INTEGER", nullable: true),
                    CaCO3 = table.Column<uint>(type: "INTEGER", nullable: true),
                    Price = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackdCharacterItem", x => new { x.UserId, x.Slot });
                    table.ForeignKey(
                        name: "FK_BackdCharacterItem_BackdCharacters_UserId",
                        column: x => x.UserId,
                        principalTable: "BackdCharacters",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackdCharacterParameter",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackdCharacterParameter", x => new { x.UserId, x.Index });
                    table.ForeignKey(
                        name: "FK_BackdCharacterParameter_BackdCharacters_UserId",
                        column: x => x.UserId,
                        principalTable: "BackdCharacters",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackdCharacterItemComment",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackdCharacterItemComment", x => new { x.UserId, x.Slot, x.Index });
                    table.ForeignKey(
                        name: "FK_BackdCharacterItemComment_BackdCharacterItem_UserId_Slot",
                        columns: x => new { x.UserId, x.Slot },
                        principalTable: "BackdCharacterItem",
                        principalColumns: new[] { "UserId", "Slot" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackdCharacterItemParameter",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Secret = table.Column<bool>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackdCharacterItemParameter", x => new { x.UserId, x.Slot, x.Secret, x.Index });
                    table.ForeignKey(
                        name: "FK_BackdCharacterItemParameter_BackdCharacterItem_UserId_Slot",
                        columns: x => new { x.UserId, x.Slot },
                        principalTable: "BackdCharacterItem",
                        principalColumns: new[] { "UserId", "Slot" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException(
                "Structured character storage is a forward-only migration. Restore a pre-migration database backup to downgrade.");
        }
    }
}
