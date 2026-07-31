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
        //Task<string> LoginAsync(UserLoginDTO request);
    }
}