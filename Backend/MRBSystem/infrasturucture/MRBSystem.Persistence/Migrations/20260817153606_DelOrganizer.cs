using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRBSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DelOrganizer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_AspNetUsers_OrganizerId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_OrganizerId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "Reservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizerId",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_OrganizerId",
                table: "Reservations",
                column: "OrganizerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_AspNetUsers_OrganizerId",
                table: "Reservations",
                column: "OrganizerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
