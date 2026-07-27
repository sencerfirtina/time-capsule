using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeCapsule.API.Data;
using TimeCapsule.API.DTO;
using TimeCapsule.API.Entities;

namespace TimeCapsule.API.Services
{
    public class SpotifyService : ISpotifyService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SpotifyService> _logger;
        public SpotifyService(AppDbContext context,ILogger<SpotifyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveOrUpdateTokenAsync(int userId,SpotifyTokenResponseDTO tokenData)
        {
            //Test user
            var user = await _context.Users.Include(u=>u.SpotifyToken).FirstOrDefaultAsync(u=>u.Id == 1);

            //will add user null control

            if(user!.SpotifyToken != null)
            {
                user.SpotifyToken.AccessToken = tokenData.AccessToken;
                user.SpotifyToken.RefreshToken = tokenData.RefreshToken;
                user.SpotifyToken.ExpirationDate = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
            }
            else
            {
                var newSpotifyToken = new UserSpotifyToken
                {
                    AccessToken = tokenData.AccessToken,
                    RefreshToken = tokenData.RefreshToken,
                    ExpirationDate = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn)
                };
                user.SpotifyToken = newSpotifyToken;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<string?> GetAccessTokenAsync(int userId)
        {
            var user = await _context.Users.Include(u=>u.SpotifyToken).FirstOrDefaultAsync(u=>u.Id == 1);
            if(user?.SpotifyToken == null)
            {
                _logger.LogError("Kullanıcı veya Spotify bağlantısı bulunamadı. Önce login yapın");
                return null;
            }

            return user.SpotifyToken.AccessToken;
        }
    }
}