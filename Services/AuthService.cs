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
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;

namespace TimeCapsule.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        public AuthService(AppDbContext context,IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

        public async Task<(bool isSuccess, string? token, string? errorMessage)> LoginAsync(UserLoginDTO request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u=>u.Email == request.Email);

            if (user == null)
            {
                string message = "Invalid email address or password";
                return (false,null,message);
            }

            var passwordCorrect = BCrypt.Net.BCrypt.Verify(request.Password,user.PasswordHash);

            if (!passwordCorrect)
            {
                string message = "Invalid email address or password";
                return (false,null,message);
            }

            var token = GenerateJwtToken(user);
            return (true,token,null);
        }

        private string GenerateJwtToken(User user)
        {
            string secretKey = _configuration["JwtSettings:SecretKey"]!;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("Username", user.Username)
            };

            var token = new JwtSecurityToken
            (
                issuer: null,
                audience: null,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            string jwtString = tokenHandler.WriteToken(token);

            return jwtString;
        }
        
    }
}