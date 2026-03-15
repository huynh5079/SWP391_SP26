using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdatefieldofBudgetProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualAmount",
                table: "BudgetProposal");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "ExpenseReceipt",
                newName: "ActualAmount");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BudgetProposal",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "BudgetProposal",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "BudgetProposal",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "BudgetProposal",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetProposal_ApprovedBy",
                table: "BudgetProposal",
                column: "ApprovedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetProposal_Approver",
                table: "BudgetProposal",
                column: "ApprovedBy",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetProposal_Approver",
                table: "BudgetProposal");

            migrationBuilder.DropIndex(
                name: "IX_BudgetProposal_ApprovedBy",
                table: "BudgetProposal");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "BudgetProposal");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "BudgetProposal");

            migrationBuilder.RenameColumn(
                name: "ActualAmount",
                table: "ExpenseReceipt",
                newName: "Amount");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BudgetProposal",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "BudgetProposal",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualAmount",
                table: "BudgetProposal",
                type: "decimal(18,2)",
                nullable: true,
                defaultValue: 0m);
        }
    }
}
