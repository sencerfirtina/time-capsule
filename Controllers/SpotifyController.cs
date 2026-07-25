using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TimeCapsule.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpotifyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public SpotifyController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            string clientId = _configuration["Spotify:ClientId"]!;
            string redirectUri = _configuration["Spotify:CallbackUrl"]!;
            string encodedRedirectUri = Uri.EscapeDataString(redirectUri);
            string scope = "user-read-currently-playing";
            string spotifyAuthUrl = $"https://accounts.spotify.com/authorize?response_type=code&client_id={clientId}&scope={scope}&redirect_uri={encodedRedirectUri}";
            Console.WriteLine("DİKKAT! SPOTIFY'A GİDEN REDIRECT URI: " + redirectUri);
            Console.WriteLine("DİKKAT! SPOTIFY'A GİDEN TAM URL: " + spotifyAuthUrl);
            return Redirect(spotifyAuthUrl);
        }

        [HttpGet("callback")]
        public IActionResult Callback(string? code, string? error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest("Spotify yetkilendirilmesi reddedildi:" + error);       
            }
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Gerekli veri alınamadı!!");
            }
            return Ok(new {Code = code});
        }
    }
}