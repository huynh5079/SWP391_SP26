using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddSentimentFieldsToFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Asessment",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Assessment_Text",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Content",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content_Text",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Instructor",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructor_Text",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Label",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label_Text",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Technical",
                table: "Feedback",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Technical_Text",
                table: "Feedback",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Asessment",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Assessment_Text",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Content_Text",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Instructor",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Instructor_Text",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Label_Text",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Technical",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "Technical_Text",
                table: "Feedback");
        }
    }
}
