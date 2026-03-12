using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizerQuestionBankConstraintAndQuizSetFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizerId",
                table: "QuestionBank",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileQuiz",
                table: "QuizSet",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizerId",
                table: "QuizSet",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_OrganizerId",
                table: "QuestionBank",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "UIX_QuizSet_Organizer",
                table: "QuizSet",
                column: "OrganizerId",
                unique: true,
                filter: "[DeletedAt] IS NULL AND [OrganizerId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionBank_Organizer",
                table: "QuestionBank",
                column: "OrganizerId",
                principalTable: "StaffProfile",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizSet_Organizer",
                table: "QuizSet",
                column: "OrganizerId",
                principalTable: "StaffProfile",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionBank_Organizer",
                table: "QuestionBank");

            migrationBuilder.DropForeignKey(
                name: "FK_QuizSet_Organizer",
                table: "QuizSet");

            migrationBuilder.DropIndex(
                name: "IX_QuestionBank_OrganizerId",
                table: "QuestionBank");

            migrationBuilder.DropIndex(
                name: "UIX_QuizSet_Organizer",
                table: "QuizSet");

            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "QuestionBank");

            migrationBuilder.DropColumn(
                name: "FileQuiz",
                table: "QuizSet");

            migrationBuilder.DropColumn(
                name: "OrganizerId",
                table: "QuizSet");
        }
    }
}
