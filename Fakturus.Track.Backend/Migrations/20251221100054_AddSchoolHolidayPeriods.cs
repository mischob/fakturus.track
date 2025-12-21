using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fakturus.Track.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolHolidayPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchoolHolidayPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolHolidayPeriods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolHolidayPeriods_UserId",
                table: "SchoolHolidayPeriods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolHolidayPeriods_UserId_Year",
                table: "SchoolHolidayPeriods",
                columns: new[] { "UserId", "Year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolHolidayPeriods");
        }
    }
}
