using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSemesterDataForEnumName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [Semester]
SET [Year] = CASE
    WHEN [Year] IS NULL OR [Year] = 0
        THEN COALESCE(TRY_CONVERT(int, RIGHT([Name], 4)), YEAR(ISNULL([StartDate], GETDATE())))
    ELSE [Year]
END;

UPDATE [Semester]
SET [Name] = CASE
    WHEN [Name] LIKE 'Spring%' THEN 'Spring'
    WHEN [Name] LIKE 'Summer%' THEN 'Summer'
    WHEN [Name] LIKE 'Fall%' THEN 'Fall'
    ELSE [Name]
END;

UPDATE [Semester]
SET [Code] = CASE
    WHEN [Code] IS NULL OR LTRIM(RTRIM([Code])) = '' THEN
        CASE [Name]
            WHEN 'Spring' THEN CONCAT('SP', [Year])
            WHEN 'Summer' THEN CONCAT('SU', [Year])
            WHEN 'Fall' THEN CONCAT('FA', [Year])
            ELSE [Code]
        END
    ELSE [Code]
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data normalization migration - no down action.
        }
    }
}
