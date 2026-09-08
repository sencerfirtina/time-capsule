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
using Microsoft.AspNetCore.Authorization;
using TimeCapsule.API.Extensions;

namespace TimeCapsule.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpotifyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ISpotifyService _spotifyService;
        private readonly ICapsuleService _capsuleService;
        public SpotifyController(IConfiguration configuration,ISpotifyService spotifyService,ICapsuleService capsuleService)
        {
            _configuration = configuration;
            _spotifyService = spotifyService;
            _capsuleService = capsuleService;
        }

        [Authorize]
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

        [Authorize]
        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string? code, string? error)
        {

            int currentUserId = User.GetUserId();

            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest("Spotify authorization was denied" + error);       
            }
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("The required data could not be retrieved");
            }

            await _spotifyService.ExchangeCodeForTokenAsync(currentUserId,code);
            return Ok("Token saved successfully");
        }
    }
}