using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateQuestionSetStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop column on Event only if it exists (some databases may already be migrated)
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.columns c
    WHERE c.[name] = N'QuestionSetStatus' AND c.[object_id] = OBJECT_ID(N'[Event]')
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Event]') AND [c].[name] = N'QuestionSetStatus');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Event] DROP CONSTRAINT [' + @var0 + ']');
    ALTER TABLE [Event] DROP COLUMN [QuestionSetStatus];
END");

            // Add column to EventQuiz (int) if not exists
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns c
    WHERE c.[name] = N'QuestionSetStatus' AND c.[object_id] = OBJECT_ID(N'[EventQuiz]')
)
BEGIN
    ALTER TABLE [EventQuiz] ADD [QuestionSetStatus] int NOT NULL CONSTRAINT DF_EventQuiz_QuestionSetStatus DEFAULT(0);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionSetStatus",
                table: "EventQuiz");

            migrationBuilder.AddColumn<string>(
                name: "QuestionSetStatus",
                table: "Event",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Available");
        }
    }
}
