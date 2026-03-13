using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly AEMSContext _ctx;
        public IUserRepository Users { get; }
        public IChatRepository ChatRepository { get; }
        public IGenericRepository<StudentProfile> StudentProfiles { get; }
        public IGenericRepository<StaffProfile> StaffProfiles { get; }
        public IGenericRepository<Role> Roles { get; }
        public IGenericRepository<SystemErrorLog> SystemErrorLogs { get; }
        public IGenericRepository<Notification> Notifications { get; }
        public IGenericRepository<Event> Events { get; }
        public IGenericRepository<EventAgenda> EventAgenda { get; }
        public IGenericRepository<EventDocument> EventDocuments { get; }
        public IGenericRepository<Topic> Topics { get; }
        public IGenericRepository<Location> Locations { get; }
        public IGenericRepository<Semester> Semesters { get; }
        public IGenericRepository<Department> Departments { get; }

		public IGenericRepository<EventWaitlist> EventWaitlist{get;}
		public IGenericRepository<ApprovalLog> EventApprovalLogs { get; }
		// ✅ StudentFeature
		public IGenericRepository<Ticket> Tickets { get; }
		public IGenericRepository<Feedback> Feedbacks { get; }
        public IGenericRepository<CheckInHistory> CheckInHistories { get; }
        public IGenericRepository<EventQuiz> EventQuiz { get; }
		public IGenericRepository<EventQuizQuestion> EventQuizQuestions { get; }
		public IGenericRepository<QuestionBank> QuestionBanks { get; }
		public IGenericRepository<QuizSet> QuizSets { get; }
		public IGenericRepository<QuizSetQuestion> QuizSetQuestions { get; }
		public IGenericRepository<StudentAnswer> StudentAnswers { get; }

		// Teams
		public IGenericRepository<EventTeam> EventTeams { get; }
		public IGenericRepository<TeamMember> TeamMembers { get; }
		// Expose Events as EventRepository for backward compatibility
		public UnitOfWork(AEMSContext ctx, IUserRepository users, IChatRepository chatRepository)
        {
            _ctx = ctx;
            Users = users;
            ChatRepository = chatRepository;
            StudentProfiles = new GenericRepository<StudentProfile>(_ctx);
            StaffProfiles = new GenericRepository<StaffProfile>(_ctx);
            Roles = new GenericRepository<Role>(_ctx);
            SystemErrorLogs = new GenericRepository<SystemErrorLog>(_ctx);
            Notifications = new GenericRepository<Notification>(_ctx);
			// ✅ Update
			Events = new GenericRepository<Event>(_ctx);
		    EventAgenda = new GenericRepository<EventAgenda>(_ctx);
			EventDocuments = new GenericRepository<EventDocument>(_ctx);
			Topics = new GenericRepository<Topic>(_ctx);
			Locations = new GenericRepository<Location>(_ctx);
			Semesters = new GenericRepository<Semester>(_ctx);
			Departments = new GenericRepository<Department>(_ctx);
            EventWaitlist = new GenericRepository<EventWaitlist>(_ctx);
            EventApprovalLogs = new GenericRepository<ApprovalLog>(_ctx);
			Tickets = new GenericRepository<Ticket>(_ctx);
			Feedbacks = new GenericRepository<Feedback>(_ctx);
            CheckInHistories = new GenericRepository<CheckInHistory>(_ctx);
            EventQuiz = new GenericRepository<EventQuiz>(_ctx);
			EventQuizQuestions = new GenericRepository<EventQuizQuestion>(_ctx);
			QuestionBanks = new GenericRepository<QuestionBank>(_ctx);
			QuizSets = new GenericRepository<QuizSet>(_ctx);
			QuizSetQuestions = new GenericRepository<QuizSetQuestion>(_ctx);
			StudentAnswers = new GenericRepository<StudentAnswer>(_ctx);
			EventTeams = new GenericRepository<EventTeam>(_ctx);
			TeamMembers = new GenericRepository<TeamMember>(_ctx);
			//
		}

        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync()
            => await _ctx.Database.BeginTransactionAsync();

		public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
			=> await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.BeginTransactionAsync(_ctx.Database, isolationLevel, CancellationToken.None);

        public void Dispose() => _ctx.Dispose();
    }

}
