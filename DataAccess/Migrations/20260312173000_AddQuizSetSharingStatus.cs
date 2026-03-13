using DataAccess.Entities;
using DataAccess.Enum;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AEMSContext))]
    [Migration("20260312173000_AddQuizSetSharingStatus")]
    public partial class AddQuizSetSharingStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SharingStatus",
                table: "QuizSet",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: QuizSetVisibilityEnum.Private.ToString());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SharingStatus",
                table: "QuizSet");
        }
    }
}
