using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTeamAndAgendaSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_TeamMember_Team_Student",
                table: "TeamMember");

            migrationBuilder.RenameColumn(
                name: "SpeakerName",
                table: "EventAgenda",
                newName: "SpeakerInfo");

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "TeamMember",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "StaffId",
                table: "TeamMember",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffSpeakerId",
                table: "EventAgenda",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentSpeakerId",
                table: "EventAgenda",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_StaffId",
                table: "TeamMember",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TeamId",
                table: "TeamMember",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAgenda_StaffSpeakerId",
                table: "EventAgenda",
                column: "StaffSpeakerId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAgenda_StudentSpeakerId",
                table: "EventAgenda",
                column: "StudentSpeakerId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventAgenda_StaffProfile_StaffSpeakerId",
                table: "EventAgenda",
                column: "StaffSpeakerId",
                principalTable: "StaffProfile",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventAgenda_StudentProfile_StudentSpeakerId",
                table: "EventAgenda",
                column: "StudentSpeakerId",
                principalTable: "StudentProfile",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK__TeamMembe_StaffId",
                table: "TeamMember",
                column: "StaffId",
                principalTable: "StaffProfile",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventAgenda_StaffProfile_StaffSpeakerId",
                table: "EventAgenda");

            migrationBuilder.DropForeignKey(
                name: "FK_EventAgenda_StudentProfile_StudentSpeakerId",
                table: "EventAgenda");

            migrationBuilder.DropForeignKey(
                name: "FK__TeamMembe_StaffId",
                table: "TeamMember");

            migrationBuilder.DropIndex(
                name: "IX_TeamMember_StaffId",
                table: "TeamMember");

            migrationBuilder.DropIndex(
                name: "IX_TeamMember_TeamId",
                table: "TeamMember");

            migrationBuilder.DropIndex(
                name: "IX_EventAgenda_StaffSpeakerId",
                table: "EventAgenda");

            migrationBuilder.DropIndex(
                name: "IX_EventAgenda_StudentSpeakerId",
                table: "EventAgenda");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "TeamMember");

            migrationBuilder.DropColumn(
                name: "StaffSpeakerId",
                table: "EventAgenda");

            migrationBuilder.DropColumn(
                name: "StudentSpeakerId",
                table: "EventAgenda");

            migrationBuilder.RenameColumn(
                name: "SpeakerInfo",
                table: "EventAgenda",
                newName: "SpeakerName");

            migrationBuilder.AlterColumn<string>(
                name: "StudentId",
                table: "TeamMember",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UIX_TeamMember_Team_Student",
                table: "TeamMember",
                columns: new[] { "TeamId", "StudentId" },
                unique: true);
        }
    }
}
