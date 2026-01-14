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


        public UnitOfWork(AEMSContext ctx)
        {
            _ctx = ctx;
        }

        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync()
            => await _ctx.Database.BeginTransactionAsync();

        public void Dispose() => _ctx.Dispose();
    }

}
