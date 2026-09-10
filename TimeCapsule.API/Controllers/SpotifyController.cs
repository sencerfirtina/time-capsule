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
using System.Security.Claims;
using TimeCapsule.API.Exceptions;
using Microsoft.IdentityModel.Tokens;

namespace TimeCapsule.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpotifyController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ISpotifyService _spotifyService;
        private readonly ICapsuleService _capsuleService;
        private readonly IAuthService _authService;
        public SpotifyController(IConfiguration configuration,ISpotifyService spotifyService,ICapsuleService capsuleService,IAuthService authService)
        {
            _configuration = configuration;
            _spotifyService = spotifyService;
            _capsuleService = capsuleService;
            _authService = authService;
        }

        [Authorize]
        [HttpGet("login-url")]
        public IActionResult Login()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (String.IsNullOrEmpty(userId))
            {
                throw new UserNotFoundException(int.Parse(userId!));
            }
            string stateJwt = _authService.GenerateStateToken(userId,5);

            string clientId = _configuration["Spotify:ClientId"]!;
            string redirectUri = _configuration["Spotify:CallbackUrl"]!;
            string encodedRedirectUri = Uri.EscapeDataString(redirectUri);
            string scope = "user-read-currently-playing";
            string spotifyAuthUrl = $"https://accounts.spotify.com/authorize?response_type=code&client_id={clientId}&scope={scope}&redirect_uri={encodedRedirectUri}&state={stateJwt}";
            return Ok(new { Url = spotifyAuthUrl });
        }

        [AllowAnonymous]
        [HttpGet("callback")]
        public async Task<IActionResult> SpotifyCallback([FromQuery] string code,[FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return BadRequest("Missing Parameter");
            }

            string? extractedUserId = "";

            
            extractedUserId = _authService.ValidateStateTokenAndGetUserId(state);
            
            if (String.IsNullOrEmpty(extractedUserId))
            {
                return Unauthorized("Invalid or expired authorization request.");
            }

            if (!int.TryParse(extractedUserId,out int userId))
            {
                return BadRequest("The ID could not be transformed correctly.");
            }

            await _spotifyService.ExchangeCodeForTokenAsync(userId,code);
            return Ok("Token saved successfully");
        }
    }
}