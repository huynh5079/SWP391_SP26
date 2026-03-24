using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStatusAndMaximumSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_StudentQuizScore_EventQuiz_Student",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "SubmitStatus",
                table: "StudentQuizScore");

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "StudentQuizScore",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttemptSubmission",
                table: "EventQuiz",
                type: "int",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SubmitStatus",
                table: "EventQuiz",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NotSubmitted");

            migrationBuilder.CreateIndex(
                name: "UIX_StudentQuizScore_EventQuiz_Student",
                table: "StudentQuizScore",
                columns: new[] { "EventQuizId", "StudentId", "AttemptNumber" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [EventQuizId] IS NOT NULL AND [StudentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_StudentQuizScore_EventQuiz_Student",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "MaxAttemptSubmission",
                table: "EventQuiz");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "SubmitStatus",
                table: "EventQuiz");

            migrationBuilder.AddColumn<string>(
                name: "SubmitStatus",
                table: "StudentQuizScore",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.CreateIndex(
                name: "UIX_StudentQuizScore_EventQuiz_Student",
                table: "StudentQuizScore",
                columns: new[] { "EventQuizId", "StudentId" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [EventQuizId] IS NOT NULL AND [StudentId] IS NOT NULL");
        }
    }
}
