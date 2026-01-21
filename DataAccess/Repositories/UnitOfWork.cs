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

        public UnitOfWork(AEMSContext ctx, IUserRepository users)
        {
            _ctx = ctx;
            Users = users;
            StudentProfiles = new GenericRepository<StudentProfile>(_ctx);
            StaffProfiles = new GenericRepository<StaffProfile>(_ctx);
            Roles = new GenericRepository<Role>(_ctx);
        }

        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync()
            => await _ctx.Database.BeginTransactionAsync();

        public void Dispose() => _ctx.Dispose();
    }

}
