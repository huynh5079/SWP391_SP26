using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActivityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: chỉ tạo bảng nếu chưa tồn tại
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserActivityLog')
BEGIN
    CREATE TABLE [UserActivityLog] (
        [Id]          NVARCHAR(450)  NOT NULL,
        [UserId]      NVARCHAR(450)  NOT NULL,
        [ActionType]  NVARCHAR(MAX)  NOT NULL,
        [TargetId]    NVARCHAR(450)  NULL,
        [TargetType]  NVARCHAR(MAX)  NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [CreatedAt]   DATETIME2      NOT NULL,
        [UpdatedAt]   DATETIME2      NOT NULL,
        [DeletedAt]   DATETIME2      NULL,
        [CreatedBy]   NVARCHAR(450)  NULL,
        [UpdatedBy]   NVARCHAR(450)  NULL,
        [RowVersion]  ROWVERSION     NOT NULL,
        CONSTRAINT [PK_UserActivityLog] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserActivityLog_User] FOREIGN KEY ([UserId])
            REFERENCES [User]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_UserActivityLog_UserId]
        ON [UserActivityLog]([UserId]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserActivityLog");
        }
    }
}
