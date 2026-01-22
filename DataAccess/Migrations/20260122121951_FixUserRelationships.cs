using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixUserRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__User__Id__02084FDA",
                table: "User");

            migrationBuilder.DropForeignKey(
                name: "FK__User__Id__03F0984C",
                table: "User");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_StudentProfile_UserId",
                table: "StudentProfile");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_StaffProfile_UserId",
                table: "StaffProfile");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffProfile_User_UserId",
                table: "StaffProfile",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProfile_User_UserId",
                table: "StudentProfile",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffProfile_User_UserId",
                table: "StaffProfile");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentProfile_User_UserId",
                table: "StudentProfile");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_StudentProfile_UserId",
                table: "StudentProfile",
                column: "UserId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_StaffProfile_UserId",
                table: "StaffProfile",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__User__Id__02084FDA",
                table: "User",
                column: "Id",
                principalTable: "StudentProfile",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__User__Id__03F0984C",
                table: "User",
                column: "Id",
                principalTable: "StaffProfile",
                principalColumn: "UserId");
        }
    }
}
