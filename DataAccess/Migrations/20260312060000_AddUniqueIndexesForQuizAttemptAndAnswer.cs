using DataAccess.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AEMSContext))]
    [Migration("20260312060000_AddUniqueIndexesForQuizAttemptAndAnswer")]
    public partial class AddUniqueIndexesForQuizAttemptAndAnswer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UIX_StudentQuizScore_EventQuiz_Student",
                table: "StudentQuizScore",
                columns: new[] { "EventQuizId", "StudentId" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [EventQuizId] IS NOT NULL AND [StudentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UIX_StudentAnswer_StudentQuizScore_Question",
                table: "StudentAnswer",
                columns: new[] { "StudentQuizScoreId", "QuestionBankId" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [StudentQuizScoreId] IS NOT NULL AND [QuestionBankId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_StudentQuizScore_EventQuiz_Student",
                table: "StudentQuizScore");

            migrationBuilder.DropIndex(
                name: "UIX_StudentAnswer_StudentQuizScore_Question",
                table: "StudentAnswer");
        }
    }
}
