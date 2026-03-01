using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed Roles: Admin, Organizer, Approver, Student
            // Only insert if they don't already exist
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleName = 'Admin')
                BEGIN
                    INSERT INTO Role (Id, RoleName, CreatedAt, UpdatedAt, DeletedAt)
                    VALUES (NEWID(), 'Admin', '{now}', '{now}', NULL)
                END
            ");

            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleName = 'Organizer')
                BEGIN
                    INSERT INTO Role (Id, RoleName, CreatedAt, UpdatedAt, DeletedAt)
                    VALUES (NEWID(), 'Organizer', '{now}', '{now}', NULL)
                END
            ");

            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleName = 'Approver')
                BEGIN
                    INSERT INTO Role (Id, RoleName, CreatedAt, UpdatedAt, DeletedAt)
                    VALUES (NEWID(), 'Approver', '{now}', '{now}', NULL)
                END
            ");

            migrationBuilder.Sql($@"
                IF NOT EXISTS (SELECT 1 FROM Role WHERE RoleName = 'Student')
                BEGIN
                    INSERT INTO Role (Id, RoleName, CreatedAt, UpdatedAt, DeletedAt)
                    VALUES (NEWID(), 'Student', '{now}', '{now}', NULL)
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seeded roles
            migrationBuilder.Sql("DELETE FROM Role WHERE RoleName IN ('Admin', 'Organizer', 'Approver', 'Student')");
        }
    }
}
