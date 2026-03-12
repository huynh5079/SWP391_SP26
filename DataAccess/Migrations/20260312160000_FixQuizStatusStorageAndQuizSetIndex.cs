using DataAccess.Entities;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(AEMSContext))]
    [Migration("20260312160000_FixQuizStatusStorageAndQuizSetIndex")]
    public partial class FixQuizStatusStorageAndQuizSetIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UIX_QuizSet_Organizer'
      AND object_id = OBJECT_ID(N'[dbo].[QuizSet]'))
BEGIN
    DROP INDEX [UIX_QuizSet_Organizer] ON [dbo].[QuizSet];
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UIX_QuizSet_Organizer_Title'
      AND object_id = OBJECT_ID(N'[dbo].[QuizSet]'))
BEGIN
    CREATE UNIQUE INDEX [UIX_QuizSet_Organizer_Title]
        ON [dbo].[QuizSet] ([OrganizerId], [Title])
        WHERE [DeletedAt] IS NULL AND [OrganizerId] IS NOT NULL AND [Title] IS NOT NULL;
END;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('EventQuiz', 'QuestionSetStatus') IS NOT NULL
BEGIN
    DECLARE @columnType sysname;
    DECLARE @defaultConstraintName sysname;

    SELECT @columnType = t.name
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    INNER JOIN sys.tables tb ON c.object_id = tb.object_id
    WHERE tb.name = 'EventQuiz'
      AND c.name = 'QuestionSetStatus';

    SELECT @defaultConstraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables tb ON tb.object_id = c.object_id
    WHERE tb.name = 'EventQuiz'
      AND c.name = 'QuestionSetStatus';

    IF @defaultConstraintName IS NOT NULL
        EXEC('ALTER TABLE [EventQuiz] DROP CONSTRAINT [' + @defaultConstraintName + ']');

    IF @columnType IN ('int', 'tinyint', 'smallint', 'bigint')
    BEGIN
        EXEC(N'ALTER TABLE [EventQuiz]
            ADD [QuestionSetStatusTemp] nvarchar(50) NOT NULL
            CONSTRAINT [DF_EventQuiz_QuestionSetStatusTemp] DEFAULT(''Available'');');

        EXEC(N'UPDATE [EventQuiz]
        SET [QuestionSetStatusTemp] = CASE TRY_CONVERT(int, [QuestionSetStatus])
            WHEN 0 THEN ''Available''
            WHEN 1 THEN ''NA''
            ELSE ''Available''
        END;');

        EXEC(N'ALTER TABLE [EventQuiz] DROP COLUMN [QuestionSetStatus];');
        EXEC(N'EXEC sp_rename ''EventQuiz.QuestionSetStatusTemp'', ''QuestionSetStatus'', ''COLUMN'';');
    END
    ELSE
    BEGIN
        UPDATE [EventQuiz]
        SET [QuestionSetStatus] = CASE
            WHEN [QuestionSetStatus] IS NULL OR LTRIM(RTRIM(CONVERT(nvarchar(50), [QuestionSetStatus]))) = '' THEN 'Available'
            ELSE CONVERT(nvarchar(50), [QuestionSetStatus])
        END;

        ALTER TABLE [EventQuiz] ALTER COLUMN [QuestionSetStatus] nvarchar(50) NOT NULL;
        ALTER TABLE [EventQuiz] ADD CONSTRAINT [DF_EventQuiz_QuestionSetStatus] DEFAULT('Available') FOR [QuestionSetStatus];
    END;
END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UIX_QuizSet_Organizer_Title'
      AND object_id = OBJECT_ID(N'[dbo].[QuizSet]'))
BEGIN
    DROP INDEX [UIX_QuizSet_Organizer_Title] ON [dbo].[QuizSet];
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UIX_QuizSet_Organizer'
      AND object_id = OBJECT_ID(N'[dbo].[QuizSet]'))
BEGIN
    CREATE UNIQUE INDEX [UIX_QuizSet_Organizer]
        ON [dbo].[QuizSet] ([OrganizerId])
        WHERE [DeletedAt] IS NULL AND [OrganizerId] IS NOT NULL;
END;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('EventQuiz', 'QuestionSetStatus') IS NOT NULL
BEGIN
    DECLARE @columnType sysname;
    DECLARE @defaultConstraintName sysname;

    SELECT @columnType = t.name
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    INNER JOIN sys.tables tb ON c.object_id = tb.object_id
    WHERE tb.name = 'EventQuiz'
      AND c.name = 'QuestionSetStatus';

    SELECT @defaultConstraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables tb ON tb.object_id = c.object_id
    WHERE tb.name = 'EventQuiz'
      AND c.name = 'QuestionSetStatus';

    IF @defaultConstraintName IS NOT NULL
        EXEC('ALTER TABLE [EventQuiz] DROP CONSTRAINT [' + @defaultConstraintName + ']');

    IF @columnType NOT IN ('int', 'tinyint', 'smallint', 'bigint')
    BEGIN
        EXEC(N'ALTER TABLE [EventQuiz]
            ADD [QuestionSetStatusTemp] int NOT NULL
            CONSTRAINT [DF_EventQuiz_QuestionSetStatusTemp] DEFAULT(0);');

        EXEC(N'UPDATE [EventQuiz]
        SET [QuestionSetStatusTemp] = CASE UPPER(LTRIM(RTRIM(CONVERT(nvarchar(50), [QuestionSetStatus]))))
            WHEN ''NA'' THEN 1
            ELSE 0
        END;');

        EXEC(N'ALTER TABLE [EventQuiz] DROP COLUMN [QuestionSetStatus];');
        EXEC(N'EXEC sp_rename ''EventQuiz.QuestionSetStatusTemp'', ''QuestionSetStatus'', ''COLUMN'';');
    END;
END;
");
        }
    }
}
