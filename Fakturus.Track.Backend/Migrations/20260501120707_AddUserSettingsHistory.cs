using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fakturus.Track.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettingsHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettingsHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    WorkDays = table.Column<int>(type: "integer", nullable: false),
                    WorkHoursPerWeek = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettingsHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettingsHistory_UserId",
                table: "UserSettingsHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettingsHistory_UserId_ValidFrom",
                table: "UserSettingsHistory",
                columns: new[] { "UserId", "ValidFrom" });

            // Backfill: every existing user gets a single open-ended history
            // row mirroring their current WorkDays / WorkHoursPerWeek so that
            // overtime calculations keep working immediately after the upgrade.
            // ValidFrom = 2000-01-01 covers any prior session date.
            migrationBuilder.Sql(@"
                INSERT INTO ""UserSettingsHistory""
                    (""Id"", ""UserId"", ""ValidFrom"", ""ValidTo"", ""WorkDays"", ""WorkHoursPerWeek"", ""CreatedAt"", ""UpdatedAt"")
                SELECT
                    gen_random_uuid(),
                    ""Id"",
                    DATE '2000-01-01',
                    NULL,
                    ""WorkDays"",
                    ""WorkHoursPerWeek"",
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM ""Users"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettingsHistory");
        }
    }
}
