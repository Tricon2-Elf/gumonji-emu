using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gumonji.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class Utf8BackdItemComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Comment7",
                table: "backd.CharacterItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment6",
                table: "backd.CharacterItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment5",
                table: "backd.CharacterItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment4",
                table: "backd.CharacterItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment3",
                table: "backd.CharacterItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment2",
                table: "backd.CharacterItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment1",
                table: "backd.CharacterItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Comment0",
                table: "backd.CharacterItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldNullable: true);
            migrationBuilder.Sql("""
                UPDATE "backd.CharacterItems" SET
                    "Comment0" = CAST("Comment0" AS TEXT),
                    "Comment1" = CAST("Comment1" AS TEXT),
                    "Comment2" = CAST("Comment2" AS TEXT),
                    "Comment3" = CAST("Comment3" AS TEXT),
                    "Comment4" = CAST("Comment4" AS TEXT),
                    "Comment5" = CAST("Comment5" AS TEXT),
                    "Comment6" = CAST("Comment6" AS TEXT),
                    "Comment7" = CAST("Comment7" AS TEXT);
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Comment7",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Comment6",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Comment5",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Comment4",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Comment3",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Comment2",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Comment1",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Comment0",
                table: "backd.CharacterItems",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
            migrationBuilder.Sql("""
                UPDATE "backd.CharacterItems" SET
                    "Comment0" = CAST("Comment0" AS BLOB),
                    "Comment1" = CAST("Comment1" AS BLOB),
                    "Comment2" = CAST("Comment2" AS BLOB),
                    "Comment3" = CAST("Comment3" AS BLOB),
                    "Comment4" = CAST("Comment4" AS BLOB),
                    "Comment5" = CAST("Comment5" AS BLOB),
                    "Comment6" = CAST("Comment6" AS BLOB),
                    "Comment7" = CAST("Comment7" AS BLOB);
                """);

        }
    }
}
