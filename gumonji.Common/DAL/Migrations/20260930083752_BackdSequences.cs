using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gumonji.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class BackdSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackdSequences",
                columns: table => new
                {
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NextId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackdSequences", x => x.Name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackdSequences");
        }
    }
}
