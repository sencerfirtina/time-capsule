using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeCapsule.API.Entities;

namespace TimeCapsule.API.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddNewUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }
        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u=>u.Email == email);
        }
        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            return await _context.Users.AnyAsync(u=>u.Username == username);
        }
        public async Task<User?> FindUserAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u=>u.Email == email);
        }
        public async Task<User?> FindUserWithSpotifyTokenAsync(int userId)
        {
            return await _context.Users.Include(u=>u.SpotifyToken).FirstOrDefaultAsync(u=>u.Id == userId);
        }
    }
}