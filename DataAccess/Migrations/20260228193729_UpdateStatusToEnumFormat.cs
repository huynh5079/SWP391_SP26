using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStatusToEnumFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Ticket] SET [Status] = 'Registered' WHERE [Status] IS NULL;");
			migrationBuilder.Sql("UPDATE [Semester] SET [Status] = 'Upcoming' WHERE [Status] IS NULL;");
			migrationBuilder.Sql("UPDATE [Event] SET [Status] = 'Draft' WHERE [Status] IS NULL;");
			migrationBuilder.Sql("UPDATE [ApprovalLog] SET [Action] = 'RequestChange' WHERE [Action] IS NULL;");
			migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "User",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Ticket",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Registered",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Semester",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Upcoming",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
			
			migrationBuilder.DropColumn(
				name: "Status",
				table: "EventWaitlist");

			migrationBuilder.AddColumn<string>(
				name: "Status",
				table: "EventWaitlist",
				type: "nvarchar(50)",
				maxLength: 50,
				nullable: false,
				defaultValue: "Waiting");

			migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Event",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "ApprovalLog",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "RequestChange",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "User",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Ticket",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Registered");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Semester",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Upcoming");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Locations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

			migrationBuilder.DropColumn(
	            name: "Status",
	            table: "EventWaitlist");

			migrationBuilder.AddColumn<int>(
				name: "Status",
				table: "EventWaitlist",
				type: "int",
				nullable: true);

			migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Event",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Draft");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "ApprovalLog",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
