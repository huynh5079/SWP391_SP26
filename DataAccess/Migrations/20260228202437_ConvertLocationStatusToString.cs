using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ConvertLocationStatusToString : Migration
    {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "StatusTemp",
				table: "Locations",
				type: "nvarchar(50)",
				maxLength: 50,
				nullable: false,
				defaultValue: "Available");

			migrationBuilder.Sql(@"
        UPDATE [Locations]
        SET [StatusTemp] =
            CASE [Status]
                WHEN 0 THEN 'Available'
                WHEN 1 THEN 'Maintenance'
                WHEN 2 THEN 'Occupied'
                WHEN 3 THEN 'Closed'
                ELSE 'Available'
            END;
    ");

			migrationBuilder.DropColumn(
				name: "Status",
				table: "Locations");

			migrationBuilder.RenameColumn(
				name: "StatusTemp",
				table: "Locations",
				newName: "Status");
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Locations",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Available");
        }
    }
}
