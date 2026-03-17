using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRatingforEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Feedback");

            migrationBuilder.AlterColumn<int>(
                name: "RatingEvent",
                table: "Feedback",
                type: "int",
                nullable: true,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                table: "Event",
                type: "decimal(3,2)",
                nullable: true,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Event");

            migrationBuilder.AlterColumn<int>(
                name: "RatingEvent",
                table: "Feedback",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                table: "Feedback",
                type: "decimal(3,2)",
                nullable: true,
                defaultValue: 0m);
        }
    }
}
