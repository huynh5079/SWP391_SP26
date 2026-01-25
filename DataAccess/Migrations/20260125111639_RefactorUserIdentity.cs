using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add Column to User first
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "User",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            // 2. Data Migration: Copy Names from Profiles to User
            migrationBuilder.Sql(@"
                UPDATE u
                SET FullName = sp.FullName
                FROM [User] u
                JOIN StudentProfile sp ON u.Id = sp.UserId
                WHERE sp.FullName IS NOT NULL
            ");

            migrationBuilder.Sql(@"
                UPDATE u
                SET FullName = sp.FullName
                FROM [User] u
                JOIN StaffProfile sp ON u.Id = sp.UserId
                WHERE sp.FullName IS NOT NULL
            ");
            
            // Set default for any remaining users (e.g. Admin) if they don't have profile or empty name
            migrationBuilder.Sql(@"UPDATE [User] SET FullName = 'System User' WHERE FullName = ''");

            // 3. Drop Columns from Profiles
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "StudentProfile");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "StaffProfile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "User");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "StudentProfile",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "StaffProfile",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
