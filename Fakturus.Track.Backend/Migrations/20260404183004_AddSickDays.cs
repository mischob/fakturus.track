using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fakturus.Track.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSickDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SickDays",
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
                    table.PrimaryKey("PK_SickDays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SickDays_Date",
                table: "SickDays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SickDays_UserId",
                table: "SickDays",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SickDays_UserId_Date",
                table: "SickDays",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SickDays");
        }
    }
}
