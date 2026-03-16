using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixBudgetItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetItem_BudgetProposal_BudgetProposalId1",
                table: "BudgetItem");

            migrationBuilder.DropIndex(
                name: "IX_BudgetItem_BudgetProposalId1",
                table: "BudgetItem");

            migrationBuilder.DropColumn(
                name: "BudgetProposalId1",
                table: "BudgetItem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BudgetProposalId1",
                table: "BudgetItem",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetItem_BudgetProposalId1",
                table: "BudgetItem",
                column: "BudgetProposalId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetItem_BudgetProposal_BudgetProposalId1",
                table: "BudgetItem",
                column: "BudgetProposalId1",
                principalTable: "BudgetProposal",
                principalColumn: "Id");
        }
    }
}
