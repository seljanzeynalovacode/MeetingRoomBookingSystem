using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRBSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildingIdToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BuildingId",
                table: "SystemSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_BuildingId",
                table: "SystemSettings",
                column: "BuildingId",
                unique: true,
                filter: "[BuildingId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemSettings_Buildings_BuildingId",
                table: "SystemSettings",
                column: "BuildingId",
                principalTable: "Buildings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemSettings_Buildings_BuildingId",
                table: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_SystemSettings_BuildingId",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                table: "SystemSettings");
        }
    }
}
