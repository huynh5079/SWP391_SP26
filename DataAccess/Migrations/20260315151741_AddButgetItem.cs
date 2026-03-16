using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddButgetItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetItem",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BudgetProposalId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BudgetProposalId1 = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetItem_BudgetProposal",
                        column: x => x.BudgetProposalId,
                        principalTable: "BudgetProposal",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BudgetItem_BudgetProposal_BudgetProposalId1",
                        column: x => x.BudgetProposalId1,
                        principalTable: "BudgetProposal",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetItem_BudgetProposalId",
                table: "BudgetItem",
                column: "BudgetProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetItem_BudgetProposalId1",
                table: "BudgetItem",
                column: "BudgetProposalId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetItem");
        }
    }
}
