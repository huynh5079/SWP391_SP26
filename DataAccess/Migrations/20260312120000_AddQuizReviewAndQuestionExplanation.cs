using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizReviewAndQuestionExplanation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowReview",
                table: "EventQuiz",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "EventQuizQuestion",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "QuestionBank",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
IF COL_LENGTH('QuestionBank', 'Difficulty') IS NOT NULL
BEGIN
    DECLARE @defaultConstraintName sysname;
    SELECT @defaultConstraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = 'QuestionBank' AND c.name = 'Difficulty';

    IF @defaultConstraintName IS NOT NULL
        EXEC('ALTER TABLE [QuestionBank] DROP CONSTRAINT [' + @defaultConstraintName + ']');

    ALTER TABLE [QuestionBank] ADD [DifficultyTemp] int NOT NULL CONSTRAINT [DF_QuestionBank_DifficultyTemp] DEFAULT(1);

    UPDATE [QuestionBank]
    SET [DifficultyTemp] = CASE UPPER(CONVERT(nvarchar(50), [Difficulty]))
        WHEN 'EASY' THEN 0
        WHEN 'HARD' THEN 2
        ELSE 1
    END;

    ALTER TABLE [QuestionBank] DROP COLUMN [Difficulty];
    EXEC sp_rename 'QuestionBank.DifficultyTemp', 'Difficulty', 'COLUMN';
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('QuestionBank', 'Difficulty') IS NOT NULL
BEGIN
    DECLARE @defaultConstraintName sysname;
    SELECT @defaultConstraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = 'QuestionBank' AND c.name = 'Difficulty';

    IF @defaultConstraintName IS NOT NULL
        EXEC('ALTER TABLE [QuestionBank] DROP CONSTRAINT [' + @defaultConstraintName + ']');

    ALTER TABLE [QuestionBank] ADD [DifficultyText] nvarchar(50) NOT NULL CONSTRAINT [DF_QuestionBank_DifficultyText] DEFAULT('Medium');

    UPDATE [QuestionBank]
    SET [DifficultyText] = CASE [Difficulty]
        WHEN 0 THEN 'Easy'
        WHEN 2 THEN 'Hard'
        ELSE 'Medium'
    END;

    ALTER TABLE [QuestionBank] DROP COLUMN [Difficulty];
    EXEC sp_rename 'QuestionBank.DifficultyText', 'Difficulty', 'COLUMN';
END;");

            migrationBuilder.DropColumn(
                name: "AllowReview",
                table: "EventQuiz");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "EventQuizQuestion");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "QuestionBank");
        }
    }
}
