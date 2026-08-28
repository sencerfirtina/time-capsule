using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCapsule.API.Entities;

namespace TimeCapsule.API.Data.Repositories
{
    public interface IUserRepository
    {
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByUsernameAsync(string username);
        Task AddNewUserAsync (User user);
        Task<bool> SaveAsync();
        Task<User?> FindUserAsync(string email);
    }
}