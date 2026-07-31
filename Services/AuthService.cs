using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using TimeCapsule.API.Data;
using TimeCapsule.API.DTO;
using TimeCapsule.API.Entities;

namespace TimeCapsule.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool isSuccess,string? errorMessage)> RegisterAsync(UserRegisterDTO request)
        {
            bool emailExists = await _context.Users.AnyAsync(u=>u.Email == request.Email);

            if (emailExists)
            {
                string message = "This email address is already in use!";
                return (false,message);
            }

            bool usernameExists = await _context.Users.AnyAsync(u=>u.Username == request.Username);
            if (usernameExists)
            {
                string message = "This username is already in use!";
                return (false,message);
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = hashedPassword
            };

            _context.Add(newUser);
            await _context.SaveChangesAsync();

            return (true,null);
        } 

        /*
        public async Task<string> LoginAsync(UserLoginDTO request)
        {
            
        }
        */
    }
}