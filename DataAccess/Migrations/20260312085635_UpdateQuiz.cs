using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuiz : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "User",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "User",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Topics",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Topics",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Ticket",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Ticket",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TeamMember",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TeamMember",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SystemErrorLog",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SystemErrorLog",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StudentQuizScore",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StudentQuizScore",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StudentProfile",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StudentProfile",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StudentAnswer",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StudentAnswer",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StaffProfile",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "StaffProfile",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Semester",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Semester",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Role",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Role",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "QuizSetQuestion",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "QuizSetQuestion",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "QuizSet",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "QuizSet",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "QuestionBank",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "QuestionBank",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Notification",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Notification",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Locations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Locations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Feedback",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Feedback",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ExpenseReceipt",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ExpenseReceipt",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EventWaitlist",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "EventWaitlist",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EventTeam",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "EventTeam",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EventReminder",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "EventReminder",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EventQuizQuestion",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "EventQuizQuestion",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EventQuiz",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "EventQuiz",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EventDocument",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "EventDocument",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "EventAgenda",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "EventAgenda",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Event",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Event",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Department",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Department",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CheckInHistory",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CheckInHistory",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ChatSession",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ChatSession",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ChatMessage",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ChatMessage",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "BudgetProposal",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "BudgetProposal",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ApprovalLog",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "ApprovalLog",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "User");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "User");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TeamMember");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TeamMember");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SystemErrorLog");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SystemErrorLog");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StudentProfile");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StudentProfile");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StaffProfile");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "StaffProfile");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Semester");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Semester");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "QuizSetQuestion");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "QuizSetQuestion");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "QuizSet");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "QuizSet");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "QuestionBank");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "QuestionBank");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ExpenseReceipt");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ExpenseReceipt");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EventWaitlist");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EventWaitlist");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EventTeam");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EventTeam");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EventReminder");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EventReminder");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EventQuizQuestion");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EventQuizQuestion");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EventQuiz");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EventQuiz");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EventDocument");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EventDocument");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EventAgenda");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EventAgenda");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CheckInHistory");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CheckInHistory");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ChatSession");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ChatSession");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BudgetProposal");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "BudgetProposal");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ApprovalLog");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ApprovalLog");
        }
    }
}
