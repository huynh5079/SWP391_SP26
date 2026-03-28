using Microsoft.EntityFrameworkCore.Storage;
using DataAccess.Entities;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Abstraction
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel);
        IUserRepository Users { get; }
        IChatRepository ChatRepository { get; }
        IGenericRepository<StudentProfile> StudentProfiles { get; }
        IGenericRepository<StaffProfile> StaffProfiles { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<SystemErrorLog> SystemErrorLogs { get; }
		IGenericRepository<Notification> Notifications { get; }
		IGenericRepository<ChatbotSession> ChatbotSessions { get; }
		IGenericRepository<ChatbotMessage> ChatbotMessages { get; }
		IGenericRepository<UserActivityLog> UserActivityLogs { get; }

		// OrganizerService
		IGenericRepository<Event> Events { get; }
        IGenericRepository<EventAgenda> EventAgenda { get; }
        IGenericRepository<EventDocument> EventDocuments { get; }
		IGenericRepository<Topic> Topics { get; }
		IGenericRepository<Location> Locations { get; }
		IGenericRepository<Semester> Semesters { get; }
		IGenericRepository<Department> Departments { get; }
        IGenericRepository<EventWaitlist> EventWaitlist { get; }
		// ApprovalService
        IGenericRepository<ApprovalLog> EventApprovalLogs { get; }

		// StudentFeature
		IGenericRepository<Ticket> Tickets { get; }
		IGenericRepository<Feedback> Feedbacks { get; }
        IGenericRepository<CheckInHistory> CheckInHistories { get; }
        // Quiz
        IGenericRepository<EventQuiz> EventQuiz { get; }
		IGenericRepository<EventQuizQuestion> EventQuizQuestions { get; }
		IGenericRepository<QuestionBank> QuestionBanks { get; }
		IGenericRepository<QuizSet> QuizSets { get; }
		IGenericRepository<QuizSetQuestion> QuizSetQuestions { get; }
		IGenericRepository<StudentQuizScore> StudentQuizScores { get; }
		IGenericRepository<StudentAnswer> StudentAnswers { get; }

		// Teams
		IGenericRepository<EventTeam> EventTeams { get; }
		IGenericRepository<TeamMember> TeamMembers { get; }
        // Batch delete helper for event quiz questions to avoid N roundtrips
        Task<int> DeleteEventQuizQuestionsAsync(string eventQuizId);

        // Budget and expense
        IGenericRepository<BudgetProposal> BudgetProposals { get; }
        IGenericRepository<BudgetItem> BudgetItems { get; }
        IGenericRepository<ExpenseReceipt> ExpenseReceipts { get; }
    }

}
