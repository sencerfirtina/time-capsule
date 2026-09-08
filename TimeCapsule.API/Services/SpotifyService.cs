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
using System.Text;
using Azure;
using System.Security.Principal;
using TimeCapsule.API.Exceptions;
using TimeCapsule.API.Data.Repositories;

namespace TimeCapsule.API.Services
{
    public class SpotifyService : ISpotifyService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SpotifyService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public SpotifyService(IUserRepository userRepository,ILogger<SpotifyService> logger,IHttpClientFactory httpClientFactory,IConfiguration configuration)
        {
            _userRepository = userRepository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task SaveOrUpdateTokenAsync(int userId,SpotifyTokenResponseDTO tokenData)
        {
            
            var user = await _userRepository.FindUserWithSpotifyTokenAsync(userId); 

            if (user == null)
            {
                throw new UserNotFoundException(userId);
            }

            if(user.SpotifyToken != null)
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
            await _userRepository.SaveAsync();

        }

        public async Task<string?> GetAccessTokenAsync(int userId)
        {
            var user = await _userRepository.FindUserWithSpotifyTokenAsync(userId); 
            if(user == null)
            {
                throw new UserNotFoundException(userId);
            }
            if (user.SpotifyToken == null)
            {
                throw new NotFoundException("No access to Spotify. Please log in first!");
            }            

            return user.SpotifyToken.AccessToken;
        }
    
        public async Task<(bool isSuccess, string? ErrorMessage,string? TrackId,string? TrackName,string? ArtistName)> GetCurrentlyPlayingAsync(int userId)
        {

            var client = _httpClientFactory.CreateClient();

            var accessToken = await GetAccessTokenAsync(userId);

            if (accessToken==null)
            {
                throw new Exception("Could not found access token");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",accessToken);

            var response = await client.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await RefreshAccessTokenAsync(userId);

                var newAccessToken = await GetAccessTokenAsync(userId);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",newAccessToken);

                response = await client.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Spotify API denied. Status Code: {response.StatusCode}");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return (false,"No song is playing right now; please play a song and try again",null,null,null);
            }

            string jsonResult = await response.Content.ReadAsStringAsync();

            using (JsonDocument doc = JsonDocument.Parse(jsonResult))
            {
                JsonElement root = doc.RootElement;
            
                var item = root.GetProperty("item");
                string? trackId = item.GetProperty("id").GetString();
                string? trackName = item.GetProperty("name").GetString();
                string? arstistName = item.GetProperty("artists")[0].GetProperty("name").GetString();

                return (true,null,trackId,trackName,arstistName);
            }
        }
    
        public async Task ExchangeCodeForTokenAsync(int userId,string code)
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
                throw new Exception($"Spotify denied access. Error details: {errorResult}");
            }
            string jsonResult = await response.Content.ReadAsStringAsync();

            var tokenData = JsonSerializer.Deserialize<SpotifyTokenResponseDTO>(jsonResult);

            if (tokenData == null)
            {
                throw new Exception("Could not get Token Data");
            }

            await SaveOrUpdateTokenAsync(userId,tokenData);
        }

        public async Task RefreshAccessTokenAsync(int userId)
        {
            var user = await _userRepository.FindUserWithSpotifyTokenAsync(userId);

            if (user == null) 
            {
                throw new UserNotFoundException(userId);
            }
            if (user.SpotifyToken?.RefreshToken == null)
            {
                throw new NotFoundException("The token could not be refreshed; you need to log in again!");
            }

            var client = _httpClientFactory.CreateClient();

            string spotifyRefreshTokenUrl = "https://accounts.spotify.com/api/token";

            string clientId = _configuration["Spotify:ClientId"]!;
            string clientSecret = _configuration["Spotify:ClientSecret"]!;
            string refreshToken = user.SpotifyToken.RefreshToken;

            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",authHeaderValue);

            var requestBody = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type","refresh_token"),
                new KeyValuePair<string, string>("refresh_token",refreshToken) 
            };

            var content = new FormUrlEncodedContent(requestBody);
            
            var response = await client.PostAsync(spotifyRefreshTokenUrl,content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("The Spotify Service is unavailable");
            }

            using (JsonDocument doc = JsonDocument.Parse(responseString))
            {
                var accessToken = doc.RootElement.GetProperty("access_token").GetString();

                if (doc.RootElement.TryGetProperty("refresh_token",out var freshRefreshToken))
                {
                    string newRefreshToken = freshRefreshToken.GetString()!;
                    user.SpotifyToken.RefreshToken = newRefreshToken;
                }

                if (accessToken == null)
                {
                    throw new Exception("Could not get Access Token");
                }

                user.SpotifyToken.AccessToken = accessToken;
            }

            await _userRepository.SaveAsync();        
        }
    }

}