using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using TimeCapsule.API.DTO;
using TimeCapsule.API.Data;
using Microsoft.EntityFrameworkCore;
using TimeCapsule.API.Entities;
using System.Net.Http.Headers;
using TimeCapsule.API.Services;

namespace TimeCapsule.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpotifyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISpotifyService _spotifyService;
        private readonly ICapsuleService _capsuleService;
        public SpotifyController(IConfiguration configuration,IHttpClientFactory httpClientFactory,ISpotifyService spotifyService,ICapsuleService capsuleService)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _spotifyService = spotifyService;
            _capsuleService = capsuleService;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            string clientId = _configuration["Spotify:ClientId"]!;
            string redirectUri = _configuration["Spotify:CallbackUrl"]!;
            string encodedRedirectUri = Uri.EscapeDataString(redirectUri);
            string scope = "user-read-currently-playing";
            string spotifyAuthUrl = $"https://accounts.spotify.com/authorize?response_type=code&client_id={clientId}&scope={scope}&redirect_uri={encodedRedirectUri}";
            return Redirect(spotifyAuthUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string? code, string? error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest("Spotify yetkilendirilmesi reddedildi:" + error);       
            }
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Gerekli veri alınamadı!!");
            }

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
                return BadRequest($"Spotify reddetti. Hata detayı: {errorResult}");
            }
            string jsonResult = await response.Content.ReadAsStringAsync();

            var tokenData = JsonSerializer.Deserialize<SpotifyTokenResponseDTO>(jsonResult);
            if (tokenData == null)
            {
                return BadRequest("Token okunamadı!!");
            }

            await _spotifyService.SaveOrUpdateTokenAsync(1,tokenData);
            return Ok("Token'lar başarıyla DTO'ya çevrildi ve SQL'e yazıldı!");
        }
    
        [HttpGet("currently-playing")]
        public async Task<IActionResult> GetCurrentlyPlaying()
        {

            var client = _httpClientFactory.CreateClient();

            var accessToken = await _spotifyService.GetAccessTokenAsync(1);

            if (accessToken==null)
            {
                return BadRequest("Token alınamadı!! Tekrar deneyin");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",accessToken);

            var response = await client.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest($"Spotify API Reddedildi. Durum Kodu: {response.StatusCode}");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return Ok("Şu an herhangi bir şarkı çalmıyor!! Bir şarkı açıp tekrar dene");
            }

            string jsonResult = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(jsonResult);
            JsonElement root = doc.RootElement;
            
            var item = root.GetProperty("item");
            string? trackId = item.GetProperty("id").GetString();
            //kontrol için bu ikisine gerek yok kaydederken ihtiyacımız var
            //string? trackName = item.GetProperty("name").GetString();
            //string? arstistName = item.GetProperty("artists")[0].GetProperty("name").GetString();
            
            bool didCapsuleOpened = await _capsuleService.TryUnlockSpotifyCapsuleAsync(1,trackId!);
            if (didCapsuleOpened)
            {
                return Ok("Bir kapsül açıldı hemen kontrol edinn!!");
            }
            return Ok("Bu şarkı için bir kapsülünüz yok başka şarkıları deneyinn!!");
        }
    }
}