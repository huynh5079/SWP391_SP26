using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLockExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReactivateAt",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Status_ReactivateAt",
                table: "User",
                columns: new[] { "Status", "ReactivateAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_Status_ReactivateAt",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ReactivateAt",
                table: "User");
        }
    }
}
