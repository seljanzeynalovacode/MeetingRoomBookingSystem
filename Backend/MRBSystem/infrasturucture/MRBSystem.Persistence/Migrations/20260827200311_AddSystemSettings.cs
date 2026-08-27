using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRBSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinReservationMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxReservationHours = table.Column<int>(type: "int", nullable: false),
                    BufferMinutes = table.Column<int>(type: "int", nullable: false),
                    BookingHorizonDays = table.Column<int>(type: "int", nullable: false),
                    IsActiveReservationLimitEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxActiveReservationsPerUser = table.Column<int>(type: "int", nullable: false),
                    MaxCalendarRangeDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
