using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ImproveDbContextIntegrityAndQuizSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[TR_Ticket_CheckCapacity]', N'TR') IS NOT NULL
	DROP TRIGGER [dbo].[TR_Ticket_CheckCapacity];
IF OBJECT_ID(N'[dbo].[TR_Ticket_RemoveWaitlist]', N'TR') IS NOT NULL
	DROP TRIGGER [dbo].[TR_Ticket_RemoveWaitlist];");

			migrationBuilder.DropIndex(
				name: "UIX_QuizSet_Organizer",
				table: "QuizSet");

			migrationBuilder.AlterColumn<string>(
				name: "Status",
				table: "StudentQuizScore",
				type: "nvarchar(50)",
				maxLength: 50,
				nullable: false,
				defaultValue: "NotStarted",
				oldClrType: typeof(string),
				oldType: "nvarchar(50)",
				oldMaxLength: 50,
				oldDefaultValue: "Submitted");

			migrationBuilder.CreateTable(
				name: "EventQuizQuestion",
				columns: table => new
				{
					Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
					CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
					UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
					DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
					EventQuizId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
					QuestionBankId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
					QuestionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
					OptionA = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
					OptionB = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
					OptionC = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
					OptionD = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
					CorrectAnswer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
					Difficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
					ScorePoint = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
					OrderIndex = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_EventQuizQuestion", x => x.Id);
					table.ForeignKey(
						name: "FK_EventQuizQuestion_EventQuiz",
						column: x => x.EventQuizId,
						principalTable: "EventQuiz",
						principalColumn: "Id");
					table.ForeignKey(
						name: "FK_EventQuizQuestion_QuestionBank",
						column: x => x.QuestionBankId,
						principalTable: "QuestionBank",
						principalColumn: "Id");
				});

			migrationBuilder.CreateIndex(
				name: "IX_ChatMessage_SessionId_CreatedAt",
				table: "ChatMessage",
				columns: new[] { "SessionId", "CreatedAt" });

			migrationBuilder.CreateIndex(
				name: "IX_EventQuizQuestion_EventQuiz_OrderIndex",
				table: "EventQuizQuestion",
				columns: new[] { "EventQuizId", "OrderIndex" },
				unique: true);

			migrationBuilder.CreateIndex(
				name: "IX_EventQuizQuestion_QuestionBankId",
				table: "EventQuizQuestion",
				column: "QuestionBankId");

			migrationBuilder.CreateIndex(
				name: "UIX_EventQuizQuestion_EventQuiz_QuestionBank",
				table: "EventQuizQuestion",
				columns: new[] { "EventQuizId", "QuestionBankId" },
				unique: true,
				filter: "[DeletedAt] IS NULL AND [EventQuizId] IS NOT NULL AND [QuestionBankId] IS NOT NULL");

			migrationBuilder.CreateIndex(
				name: "UIX_QuizSet_Organizer_Title",
				table: "QuizSet",
				columns: new[] { "OrganizerId", "Title" },
				unique: true,
				filter: "[DeletedAt] IS NULL AND [OrganizerId] IS NOT NULL AND [Title] IS NOT NULL");

			migrationBuilder.CreateIndex(
				name: "UIX_TeamMember_Team_Student",
				table: "TeamMember",
				columns: new[] { "TeamId", "StudentId" },
				unique: true,
				filter: "[DeletedAt] IS NULL AND [TeamId] IS NOT NULL AND [StudentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropIndex(
				name: "IX_ChatMessage_SessionId_CreatedAt",
				table: "ChatMessage");

			migrationBuilder.DropIndex(
				name: "UIX_QuizSet_Organizer_Title",
				table: "QuizSet");

			migrationBuilder.DropIndex(
				name: "UIX_TeamMember_Team_Student",
				table: "TeamMember");

			migrationBuilder.DropTable(
				name: "EventQuizQuestion");

			migrationBuilder.AlterColumn<string>(
				name: "Status",
				table: "StudentQuizScore",
				type: "nvarchar(50)",
				maxLength: 50,
				nullable: false,
				defaultValue: "Submitted",
				oldClrType: typeof(string),
				oldType: "nvarchar(50)",
				oldMaxLength: 50,
				oldDefaultValue: "NotStarted");

			migrationBuilder.CreateIndex(
				name: "UIX_QuizSet_Organizer",
				table: "QuizSet",
				column: "OrganizerId",
				unique: true,
				filter: "[DeletedAt] IS NULL AND [OrganizerId] IS NOT NULL");
        }
    }
}
