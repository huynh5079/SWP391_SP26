using Microsoft.EntityFrameworkCore.Storage;
using DataAccess.Entities;
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
        IUserRepository Users { get; }
        IChatRepository ChatRepository { get; }
        IGenericRepository<StudentProfile> StudentProfiles { get; }
        IGenericRepository<StaffProfile> StaffProfiles { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<SystemErrorLog> SystemErrorLogs { get; }
		IGenericRepository<Notification> Notifications { get; }
		
		

		// ✅ Update for OrganizerService
		IGenericRepository<Event> Events { get; }
        IGenericRepository<EventAgenda> EventAgenda { get; }
        IGenericRepository<EventDocument> EventDocuments { get; }
		IGenericRepository<Topic> Topics { get; }
		IGenericRepository<Location> Locations { get; }
		IGenericRepository<Semester> Semesters { get; }
		IGenericRepository<Department> Departments { get; }
        IGenericRepository<EventWaitlist> EventWaitlist { get; }
		//Update for ApprovalService
        IGenericRepository<ApprovalLog> EventApprovalLogs { get; }

		// ✅ Update for StudentFeature
		IGenericRepository<Ticket> Tickets { get; }
		IGenericRepository<Feedback> Feedbacks { get; }
        IGenericRepository<CheckInHistory> CheckInHistories { get; }
        //Quiz
        IGenericRepository<QuizQuestion> QuizQuestion { get; }
        IGenericRepository<EventQuiz> EventQuiz { get; }

	}

}
