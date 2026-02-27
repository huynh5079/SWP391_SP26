using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEventWaitlistField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OfferedAt",
                table: "EventWaitlist",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "EventWaitlist",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondedAt",
                table: "EventWaitlist",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "EventWaitlist",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfferedAt",
                table: "EventWaitlist");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "EventWaitlist");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "EventWaitlist");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EventWaitlist");
        }
    }
}
