using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fakturus.Track.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkDaysToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkDays",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 31);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkDays",
                table: "Users");
        }
    }
}
