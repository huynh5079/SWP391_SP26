using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore.Storage;
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
        public IGenericRepository<StudentProfile> StudentProfiles { get; }
        public IGenericRepository<StaffProfile> StaffProfiles { get; }
        public IGenericRepository<Role> Roles { get; }
        public IGenericRepository<SystemErrorLog> SystemErrorLogs { get; }
        public IGenericRepository<Event> Events { get; }
        public IGenericRepository<EventAgenda> EventAgenda { get; }
        public IGenericRepository<Topic> Topics { get; }
        public IGenericRepository<Location> Locations { get; }
        public IGenericRepository<Semester> Semesters { get; }
        public IGenericRepository<Department> Departments { get; }

		public IGenericRepository<EventWaitlist> EventWaitlist{get;}
		public IGenericRepository<ApprovalLog> EventApprovalLogs { get; }
		// ✅ StudentFeature
		public IGenericRepository<Ticket> Tickets { get; }
		public IGenericRepository<Feedback> Feedbacks { get; }
		public UnitOfWork(AEMSContext ctx, IUserRepository users)
        {
            _ctx = ctx;
            Users = users;
            StudentProfiles = new GenericRepository<StudentProfile>(_ctx);
            StaffProfiles = new GenericRepository<StaffProfile>(_ctx);
            Roles = new GenericRepository<Role>(_ctx);
            SystemErrorLogs = new GenericRepository<SystemErrorLog>(_ctx);
			// ✅ Update
			Events = new GenericRepository<Event>(_ctx);
		    EventAgenda = new GenericRepository<EventAgenda>(_ctx);
			Topics = new GenericRepository<Topic>(_ctx);
			Locations = new GenericRepository<Location>(_ctx);
			Semesters = new GenericRepository<Semester>(_ctx);
			Departments = new GenericRepository<Department>(_ctx);
            EventWaitlist = new GenericRepository<EventWaitlist>(_ctx);
            EventApprovalLogs = new GenericRepository<ApprovalLog>(_ctx);
			Tickets = new GenericRepository<Ticket>(_ctx);
			Feedbacks = new GenericRepository<Feedback>(_ctx);
            //
		}

        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync()
            => await _ctx.Database.BeginTransactionAsync();

        public void Dispose() => _ctx.Dispose();
    }

}
