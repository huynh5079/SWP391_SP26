using DataAccess.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AEMSContext))]
    [Migration("20260312043000_RedesignEventQuizSchema")]
    public partial class RedesignEventQuizSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionBank",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TopicId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    QuestionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OptionB = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OptionC = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    OptionD = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Medium"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionBank", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionBank_Topic",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuizSet",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TopicId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizSet_Topic",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id");
                });

            migrationBuilder.AddColumn<string>(
                name: "QuizSetId",
                table: "EventQuiz",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeLimit",
                table: "EventQuiz",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EventQuizId",
                table: "StudentQuizScore",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "StudentQuizScore",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "StudentQuizScore",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "StudentQuizScore",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Submitted");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "StudentQuizScore",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuizSetQuestion",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QuizSetId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    QuestionBankId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ScorePoint = table.Column<int>(type: "int", nullable: true, defaultValue: 1),
                    OrderIndex = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizSetQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizSetQuestion_QuestionBank",
                        column: x => x.QuestionBankId,
                        principalTable: "QuestionBank",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuizSetQuestion_QuizSet",
                        column: x => x.QuizSetId,
                        principalTable: "QuizSet",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentAnswer",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentQuizScoreId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    QuestionBankId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SelectedAnswer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    ScoreEarned = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAnswer_QuestionBank",
                        column: x => x.QuestionBankId,
                        principalTable: "QuestionBank",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentAnswer_StudentQuizScore",
                        column: x => x.StudentQuizScoreId,
                        principalTable: "StudentQuizScore",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventQuiz_QuizSetId",
                table: "EventQuiz",
                column: "QuizSetId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBank_TopicId",
                table: "QuestionBank",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSet_TopicId",
                table: "QuizSet",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSetQuestion_QuestionBankId",
                table: "QuizSetQuestion",
                column: "QuestionBankId");

            migrationBuilder.CreateIndex(
                name: "UIX_QuizSetQuestion_QuizSet_Question",
                table: "QuizSetQuestion",
                columns: new[] { "QuizSetId", "QuestionBankId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_QuestionBankId",
                table: "StudentAnswer",
                column: "QuestionBankId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswer_StudentQuizScoreId",
                table: "StudentAnswer",
                column: "StudentQuizScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentQuizScore_EventQuizId",
                table: "StudentQuizScore",
                column: "EventQuizId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventQuiz_QuizSet",
                table: "EventQuiz",
                column: "QuizSetId",
                principalTable: "QuizSet",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentQuizScore_EventQuiz",
                table: "StudentQuizScore",
                column: "EventQuizId",
                principalTable: "EventQuiz",
                principalColumn: "Id");

            migrationBuilder.Sql(@"
                INSERT INTO QuizSet (Id, TopicId, Title, Description, IsActive, CreatedAt, UpdatedAt, DeletedAt)
                SELECT eq.Id,
                       e.TopicId,
                       eq.Title,
                       NULL,
                       eq.IsActive,
                       eq.CreatedAt,
                       eq.UpdatedAt,
                       eq.DeletedAt
                FROM EventQuiz eq
                LEFT JOIN Event e ON e.Id = eq.EventId
                WHERE NOT EXISTS (
                    SELECT 1 FROM QuizSet qs WHERE qs.Id = eq.Id
                );");

            migrationBuilder.Sql(@"
                UPDATE EventQuiz
                SET QuizSetId = Id
                WHERE QuizSetId IS NULL;");

            migrationBuilder.Sql(@"
                INSERT INTO QuestionBank (Id, TopicId, QuestionText, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, Difficulty, CreatedAt, UpdatedAt, DeletedAt)
                SELECT qq.Id,
                       COALESCE(qs.TopicId, e.TopicId),
                       qq.QuestionText,
                       qq.OptionA,
                       qq.OptionB,
                       qq.OptionC,
                       qq.OptionD,
                       qq.CorrectAnswer,
                       'Medium',
                       qq.CreatedAt,
                       qq.UpdatedAt,
                       qq.DeletedAt
                FROM QuizQuestion qq
                INNER JOIN EventQuiz eq ON eq.Id = qq.QuizId
                LEFT JOIN QuizSet qs ON qs.Id = eq.QuizSetId
                LEFT JOIN Event e ON e.Id = eq.EventId
                WHERE NOT EXISTS (
                    SELECT 1 FROM QuestionBank qb WHERE qb.Id = qq.Id
                );");

            migrationBuilder.Sql(@"
                WITH QuestionSource AS
                (
                    SELECT CONVERT(nvarchar(450), NEWID()) AS Id,
                           eq.QuizSetId,
                           qq.Id AS QuestionBankId,
                           ISNULL(qq.ScorePoint, 1) AS ScorePoint,
                           ROW_NUMBER() OVER (PARTITION BY eq.QuizSetId ORDER BY qq.CreatedAt, qq.Id) AS OrderIndex,
                           qq.CreatedAt,
                           qq.UpdatedAt,
                           qq.DeletedAt
                    FROM QuizQuestion qq
                    INNER JOIN EventQuiz eq ON eq.Id = qq.QuizId
                    WHERE eq.QuizSetId IS NOT NULL
                )
                INSERT INTO QuizSetQuestion (Id, QuizSetId, QuestionBankId, ScorePoint, OrderIndex, CreatedAt, UpdatedAt, DeletedAt)
                SELECT qs.Id,
                       qs.QuizSetId,
                       qs.QuestionBankId,
                       qs.ScorePoint,
                       qs.OrderIndex,
                       qs.CreatedAt,
                       qs.UpdatedAt,
                       qs.DeletedAt
                FROM QuestionSource qs
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM QuizSetQuestion x
                    WHERE x.QuizSetId = qs.QuizSetId AND x.QuestionBankId = qs.QuestionBankId
                );");

            migrationBuilder.Sql(@"
                UPDATE StudentQuizScore
                SET EventQuizId = COALESCE(EventQuizId, QuizId),
                    Score = COALESCE(Score, TotalScore),
                    StartedAt = COALESCE(StartedAt, CreatedAt),
                    SubmittedAt = COALESCE(SubmittedAt, UpdatedAt),
                    Status = COALESCE(Status, 'Submitted');");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EventQuiz_Topic')
                    ALTER TABLE [EventQuiz] DROP CONSTRAINT [FK_EventQuiz_Topic];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventQuiz_TopicId' AND object_id = OBJECT_ID(N'[EventQuiz]'))
                    DROP INDEX [IX_EventQuiz_TopicId] ON [EventQuiz];

                IF COL_LENGTH('EventQuiz', 'TopicId') IS NOT NULL
                    ALTER TABLE [EventQuiz] DROP COLUMN [TopicId];

                IF OBJECT_ID(N'[QuizQuestion]', N'U') IS NOT NULL
                    DROP TABLE [QuizQuestion];");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventQuiz_QuizSet",
                table: "EventQuiz");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentQuizScore_EventQuiz",
                table: "StudentQuizScore");

            migrationBuilder.DropTable(
                name: "QuizSetQuestion");

            migrationBuilder.DropTable(
                name: "StudentAnswer");

            migrationBuilder.DropTable(
                name: "QuizSet");

            migrationBuilder.DropTable(
                name: "QuestionBank");

            migrationBuilder.DropIndex(
                name: "IX_EventQuiz_QuizSetId",
                table: "EventQuiz");

            migrationBuilder.DropIndex(
                name: "IX_StudentQuizScore_EventQuizId",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "QuizSetId",
                table: "EventQuiz");

            migrationBuilder.DropColumn(
                name: "TimeLimit",
                table: "EventQuiz");

            migrationBuilder.DropColumn(
                name: "EventQuizId",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StudentQuizScore");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "StudentQuizScore");
        }
    }
}
