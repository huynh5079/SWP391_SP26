using System;
using BusinessLogic.DTOs.Role;
using BusinessLogic.DTOs.Role.Organizer;
using DataAccess.Enum;
using static BusinessLogic.DTOs.Organizer.BudgetProposal.BugetProposalDtos;

namespace BusinessLogic.Service.Organizer.BudgetProposal
{
    public interface IBudgetProposalService
    {
        // BudgetProposal
        Task<BudgetProposalDetailDto> GetByEventAsync(string eventId);
        Task<BudgetProposalDetailDto> CreateAsync(string organizerId, CreateBudgetProposalDto dto);
        Task SubmitForApprovalAsync(string organizerId, string proposalId);
        Task ApproveAsync(string approverId, string proposalId, string? note);
        Task RejectAsync(string approverId, string proposalId, string note);
        Task EditProposalAsync(string organizerId, string proposalId, string? title, string? description);
        Task DeleteProposalAsync(string organizerId, string proposalId);

        // BudgetItem
        Task<BudgetItemDto> AddItemAsync(string organizerId, string proposalId, CreateBudgetItemDto dto);
        Task UpdateItemAsync(string organizerId, string itemId, CreateBudgetItemDto dto);
        Task RemoveItemAsync(string organizerId, string itemId);

        // ExpenseReceipt
        Task<ExpenseReceiptDto> AddReceiptAsync(string organizerId, string proposalId, CreateExpenseReceiptDto dto);
        Task<List<ExpenseReceiptDto>> GetReceiptsByProposalAsync(string proposalId);
        Task UpdateReceiptStatusAsync(string approverId, string receiptId, ExpenseStatusEnum status);
        Task RejectReceiptAsync(string approverId, string receiptId, string eventId, string note);
        Task EditReceiptAsync(string organizerId, string receiptId, string? title, decimal actualAmount);
        Task DeleteReceiptAsync(string organizerId, string receiptId);

        // Báo cáo quyết toán
        Task<BudgetSummaryDto> GetSummaryAsync(string proposalId);
    }
}
