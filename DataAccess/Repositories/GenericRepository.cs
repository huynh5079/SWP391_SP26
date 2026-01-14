using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly AEMSContext _context;
        internal DbSet<T> _dbSet;

        public GenericRepository(AEMSContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IQueryable<T>>? includes = null)
        {
            IQueryable<T> query = _dbSet;
            if (filter != null) query = query.Where(filter);
            if (includes != null) query = includes(query);
            return await query.ToListAsync();
        }

        public async Task<T?> GetAsync(Expression<Func<T, bool>> filter,
            Func<IQueryable<T>, IQueryable<T>>? includes = null)
        {
            IQueryable<T> query = _dbSet.Where(filter);
            if (includes != null) query = includes(query);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<T?> GetByIdAsync(string id) => await _dbSet.FindAsync(id);

        public async Task CreateAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(T entity)
        {
            _dbSet.Remove(entity);
        }
    }
}
