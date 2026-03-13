using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class BigUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventQuizQuestion_QuestionBank",
                table: "EventQuizQuestion");

            migrationBuilder.DropIndex(
                name: "IX_TeamMember_TeamId",
                table: "TeamMember");

            migrationBuilder.DropIndex(
                name: "UIX_QuizSetQuestion_QuizSet_Question",
                table: "QuizSetQuestion");

            migrationBuilder.DropIndex(
                name: "IX_EventQuizQuestion_EventQuiz_OrderIndex",
                table: "EventQuizQuestion");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "User",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Topics",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Ticket",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TeamMember",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SystemErrorLog",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StudentQuizScore",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StudentProfile",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StudentAnswer",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffProfile",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Semester",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Role",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "QuizSetQuestion",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "QuizSet",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "QuestionBank",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Notification",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Locations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Locations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Feedback",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ExpenseReceipt",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EventWaitlist",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EventTeam",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EventReminder",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EventQuizQuestion",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EventQuiz",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EventDocument",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "EventAgenda",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Event",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Department",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CheckInHistory",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ChatSession",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ChatMessage",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BudgetProposal",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ApprovalLog",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "UIX_QuizSetQuestion_QuizSet_Question",
                table: "QuizSetQuestion",
                columns: new[] { "QuizSetId", "QuestionBankId" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [QuizSetId] IS NOT NULL AND [QuestionBankId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventQuizQuestion_EventQuiz_OrderIndex",
                table: "EventQuizQuestion",
                columns: new[] { "EventQuizId", "OrderIndex" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [EventQuizId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventQuizQuestion_OrderIndex_NonNegative",
                table: "EventQuizQuestion",
                sql: "[OrderIndex] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Event_Mode_Offline_MeetingUrl",
                table: "Event",
                sql: "[Mode] <> 'Offline' OR [MeetingUrl] IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Event_Mode_Online_Location",
                table: "Event",
                sql: "[Mode] <> 'Online' OR [LocationId] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_EventQuizQuestion_QuestionBank",
                table: "EventQuizQuestion",
                column: "QuestionBankId",
                principalTable: "QuestionBank",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventQuizQuestion_QuestionBank",
                table: "EventQuizQuestion");

            migrationBuilder.DropIndex(
                name: "UIX_QuizSetQuestion_QuizSet_Question",
                table: "QuizSetQuestion");

            migrationBuilder.DropIndex(
                name: "IX_EventQuizQuestion_EventQuiz_OrderIndex",
                table: "EventQuizQuestion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventQuizQuestion_OrderIndex_NonNegative",
                table: "EventQuizQuestion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Event_Mode_Offline_MeetingUrl",
                table: "Event");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Event_Mode_Online_Location",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "User");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Ticket");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TeamMember");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SystemErrorLog");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StudentProfile");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StudentAnswer");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffProfile");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Semester");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "QuizSetQuestion");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "QuizSet");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "QuestionBank");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Feedback");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ExpenseReceipt");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EventWaitlist");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EventTeam");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EventReminder");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EventQuizQuestion");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EventQuiz");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EventDocument");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "EventAgenda");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Event");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CheckInHistory");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ChatSession");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BudgetProposal");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ApprovalLog");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Locations",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TeamId",
                table: "TeamMember",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "UIX_QuizSetQuestion_QuizSet_Question",
                table: "QuizSetQuestion",
                columns: new[] { "QuizSetId", "QuestionBankId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventQuizQuestion_EventQuiz_OrderIndex",
                table: "EventQuizQuestion",
                columns: new[] { "EventQuizId", "OrderIndex" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EventQuizQuestion_QuestionBank",
                table: "EventQuizQuestion",
                column: "QuestionBankId",
                principalTable: "QuestionBank",
                principalColumn: "Id");
        }
    }
}
