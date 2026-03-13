using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFKtopic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.AddColumn<string>(
				name: "TopicId",
				table: "EventQuiz",
				type: "nvarchar(450)",
				nullable: true);

			migrationBuilder.Sql(@"
				UPDATE eq
				SET eq.TopicId = e.TopicId
				FROM EventQuiz eq
				INNER JOIN Event e ON eq.EventId = e.Id
				WHERE eq.TopicId IS NULL");

			migrationBuilder.CreateIndex(
				name: "IX_EventQuiz_TopicId",
				table: "EventQuiz",
				column: "TopicId");

			migrationBuilder.AddForeignKey(
				name: "FK_EventQuiz_Topic",
				table: "EventQuiz",
				column: "TopicId",
				principalTable: "Topics",
				principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.DropForeignKey(
				name: "FK_EventQuiz_Topic",
				table: "EventQuiz");

			migrationBuilder.DropIndex(
				name: "IX_EventQuiz_TopicId",
				table: "EventQuiz");

			migrationBuilder.DropColumn(
				name: "TopicId",
				table: "EventQuiz");
        }
    }
}
