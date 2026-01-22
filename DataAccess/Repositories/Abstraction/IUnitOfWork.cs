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
        IGenericRepository<StudentProfile> StudentProfiles { get; }
        IGenericRepository<StaffProfile> StaffProfiles { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<SystemErrorLog> SystemErrorLogs { get; }
    }

}
