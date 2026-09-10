using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCapsule.API.DTO;

namespace TimeCapsule.API.Services
{
    public interface IAuthService
    {
        Task<(bool isSuccess,string? errorMessage)> RegisterAsync(UserRegisterDTO request);
        Task<(bool isSuccess, string? token, string? errorMessage)> LoginAsync(UserLoginDTO request);
        string GenerateStateToken(string userId, int expirationMinutes);
        string? ValidateStateTokenAndGetUserId(string stateJwt);
    }
}