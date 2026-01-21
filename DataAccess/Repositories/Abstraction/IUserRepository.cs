using DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories.Abstraction
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> FindByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email);
    }
}
