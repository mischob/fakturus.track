using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fakturus.Track.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddVacationDaysAndUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VacationDaysPerYear",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkHoursPerWeek",
                table: "Users",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 40m);

            migrationBuilder.CreateTable(
                name: "VacationDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacationDays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VacationDays_Date",
                table: "VacationDays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_VacationDays_UserId",
                table: "VacationDays",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacationDays_UserId_Date",
                table: "VacationDays",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VacationDays");

            migrationBuilder.DropColumn(
                name: "VacationDaysPerYear",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkHoursPerWeek",
                table: "Users");
        }
    }
}
