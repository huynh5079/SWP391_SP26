using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChatbotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ChatbotSession",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ChatbotSession",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ChatbotSession",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ChatbotSession",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ChatbotSession",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ChatbotSession",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ChatbotSession");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ChatbotSession");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ChatbotSession");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ChatbotSession");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ChatbotSession");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ChatbotSession");
        }
    }
}
