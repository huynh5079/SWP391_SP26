using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RefactorRolesAndEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Event");

            migrationBuilder.AddColumn<string>(
                name: "LocationId",
                table: "Event",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopicId",
                table: "Event",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Event_LocationId",
                table: "Event",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Event_TopicId",
                table: "Event",
                column: "TopicId");

            migrationBuilder.AddForeignKey(
                name: "FK_Event_Location",
                table: "Event",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Event_Topic",
                table: "Event",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Event_Location",
                table: "Event");

            migrationBuilder.DropForeignKey(
                name: "FK_Event_Topic",
                table: "Event");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropIndex(
                name: "IX_Event_LocationId",
                table: "Event");

            migrationBuilder.DropIndex(
                name: "IX_Event_TopicId",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "TopicId",
                table: "Event");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Event",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
