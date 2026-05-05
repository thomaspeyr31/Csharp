using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionToListsAndCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Lists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "Lists");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Cards");
        }
    }
}

