using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using TimeCapsule.API.Data;
using TimeCapsule.API.DTO;
using TimeCapsule.API.Entities;

using System.Text.Json;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TimeCapsule.API.Services
{
    public class SpotifyService : ISpotifyService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SpotifyService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public SpotifyService(AppDbContext context,ILogger<SpotifyService> logger,IHttpClientFactory httpClientFactory,IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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
    
        public async Task<(bool isSuccess, string? ErrorMessage, string? TrackId,string? TrackName,string? ArtistName)> GetCurrentlyPlayingAsync(int userId)
        {

            var client = _httpClientFactory.CreateClient();

            var accessToken = await GetAccessTokenAsync(1);

            if (accessToken==null)
            {
                return (false,"Spotify hesabınız bağlı değil. Lütfen önce giriş yapın!",null,null,null);
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",accessToken);

            var response = await client.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");

            if (!response.IsSuccessStatusCode)
            {
                return (false,$"Spotify API Reddedildi. Durum Kodu: {response.StatusCode}",null,null,null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return (false,"Şu an herhangi bir şarkı çalmıyor!! Bir şarkı açıp tekrar deneyin",null,null,null);
            }

            string jsonResult = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(jsonResult);
            JsonElement root = doc.RootElement;
            
            var item = root.GetProperty("item");
            string? trackId = item.GetProperty("id").GetString();
            string? trackName = item.GetProperty("name").GetString();
            string? arstistName = item.GetProperty("artists")[0].GetProperty("name").GetString();

            return (true,null,trackId,trackName,arstistName);

        }
    
        public async Task<bool> ExchangeCodeForTokenAsync(int userId,string code)
        {
            var client = _httpClientFactory.CreateClient();
            string clientId = _configuration["Spotify:ClientId"]!;
            string redirectUri = _configuration["Spotify:CallbackUrl"]!;
            string clientSecret = _configuration["Spotify:ClientSecret"]!;
            var tokenRequestData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type","authorization_code"),
                new KeyValuePair<string, string>("code",code),
                new KeyValuePair<string, string>("redirect_uri",redirectUri),
                new KeyValuePair<string, string>("client_id",clientId),
                new KeyValuePair<string, string>("client_secret",clientSecret)
            };
            var requestBody = new FormUrlEncodedContent(tokenRequestData);

            var response = await client.PostAsync("https://accounts.spotify.com/api/token",requestBody);

            if (!response.IsSuccessStatusCode)
            {
                string errorResult = await response.Content.ReadAsStringAsync();
                throw new Exception($"Spotify reddetti. Hata detayı: {errorResult}");
            }
            string jsonResult = await response.Content.ReadAsStringAsync();

            var tokenData = JsonSerializer.Deserialize<SpotifyTokenResponseDTO>(jsonResult);

            if (tokenData == null)
            {
                return false;
            }

            await SaveOrUpdateTokenAsync(userId,tokenData);

            return true;
        }
    }

}