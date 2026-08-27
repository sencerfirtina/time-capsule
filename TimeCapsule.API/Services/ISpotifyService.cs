using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCapsule.API.DTO;

namespace TimeCapsule.API.Services
{
    public interface ISpotifyService
    {
        Task SaveOrUpdateTokenAsync(int userId,SpotifyTokenResponseDTO tokenData);
        Task<string?> GetAccessTokenAsync(int userId);
        Task<(bool isSuccess, string? ErrorMessage, string? TrackId,string? TrackName,string? ArtistName)> GetCurrentlyPlayingAsync(int userId);
        Task<bool> ExchangeCodeForTokenAsync(int userId,string code);
        Task<bool> RefreshAccessTokenAsync(int userId);
    }
}