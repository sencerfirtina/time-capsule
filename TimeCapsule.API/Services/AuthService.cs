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
using TimeCapsule.API.Data.Repositories;

namespace TimeCapsule.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        public AuthService(IUserRepository userRepository,IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<(bool isSuccess,string? errorMessage)> RegisterAsync(UserRegisterDTO request)
        {
            bool emailExists = await _userRepository.ExistsByEmailAsync(request.Email);

            if (emailExists)
            {
                string message = "This email address is already in use!";
                return (false,message);
            }

            bool usernameExists = await _userRepository.ExistsByUsernameAsync(request.Username);
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

            await _userRepository.AddNewUserAsync(newUser);
            await _userRepository.SaveAsync();

            return (true,null);
        } 

        public async Task<(bool isSuccess, string? token, string? errorMessage)> LoginAsync(UserLoginDTO request)
        {
            var user = await _userRepository.FindUserAsync(request.Email);

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

        public string GenerateStateToken(string userId, int expirationMinutes)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };

            string secretKey = _configuration["JwtSettings:SecretKey"]!;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken
            (
                issuer:null,
                audience:null,
                claims: claims,
                notBefore:DateTime.UtcNow,
                expires:DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials:credentials
            );
            
            var tokenHandler = new JwtSecurityTokenHandler();
            string jwtString = tokenHandler.WriteToken(token);
            return jwtString;
        }    
        
        public string? ValidateStateTokenAndGetUserId(string stateJwt)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            string secretKey = _configuration["JwtSettings:SecretKey"]!;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(stateJwt,validationParameters,out SecurityToken validatedToken);

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (String.IsNullOrEmpty(userId))
                {
                    throw new Exception("The ID could not be retrieved");
                }
                return userId;
            }
            catch
            {
                return null;
            }
        }
    }
}