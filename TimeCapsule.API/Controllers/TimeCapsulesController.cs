using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCapsule.API.Entities;
using TimeCapsule.API.Data;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TimeCapsule.API.DTO;
using TimeCapsule.API.Extensions;
using TimeCapsule.API.Services;
using Microsoft.AspNetCore.Authorization;


namespace TimeCapsule.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeCapsulesController : ControllerBase
    {
     private readonly ICapsuleService _capsuleService;
     private readonly ISpotifyService _spotifyService;
     public TimeCapsulesController(ICapsuleService capsuleService,ISpotifyService spotifyService)
        {
            _capsuleService = capsuleService;
            _spotifyService = spotifyService;
        }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateCapsule([FromBody] CreateCapsuleDTO capsuleDTO)
        {
            int currentUserId = User.GetUserId();
            var newEntity = new Entities.CapsuleEntity
            {
                EncryptedContent = capsuleDTO.EncryptedContent,
                Category = capsuleDTO.Category,
                Operator = capsuleDTO.Operator,
                TargetValue = capsuleDTO.TargetValue,
                MetaData = capsuleDTO.MetaData
            };
            await _capsuleService.CreateAndSaveCapsule(currentUserId, newEntity);
            return Ok("Kapsül başarıyla gömüldü");
        }

    [Authorize]
    [HttpPost("check-location")]
    public async Task<IActionResult> CheckLocationTriggers([FromBody] LocationCheckRequestDTO userLocation)
    {
        int currentUserId = User.GetUserId();
        var openedCapsuleIds = await _capsuleService.CheckGeoFencesAsync(currentUserId,userLocation.Latitude,userLocation.Longitude);
        return Ok(new {Message = $"{openedCapsuleIds.Count()} adet kapsül açıldı!",OpenedIds = openedCapsuleIds});
    }

    [Authorize]
    [HttpGet("check-spotify-trigger")]
    public async Task<IActionResult> CheckSpotifyTrigger()
    {
        int currentUserId = User.GetUserId();

        var currentTrack = await _spotifyService.GetCurrentlyPlayingAsync(currentUserId);

        if (!currentTrack.isSuccess)
        {
            return BadRequest(currentTrack.ErrorMessage);       
        }

        var response = await _capsuleService.TryUnlockSpotifyCapsuleAsync(currentUserId,currentTrack.TrackId!);
        
        if (response.isSuccess)
        {
            string openedCapsules = string.Join(", ", response.openedCapsuleIds!);
            return Ok(new {Message = $"Bu şarkıyı içeren {openedCapsules} id'li kapsül/kapsüller açıldı hemen kontrol edinn!!",
                           Song = currentTrack.TrackName,
                           Artist = currentTrack.ArtistName});
        }
        return Ok(new {Message = "Bu şarkı için bir kapsülünüz yok başka şarkıları deneyinn!!",
                       Song = currentTrack.TrackName,
                       Artist = currentTrack.ArtistName,
                       TrackId = currentTrack.TrackId});
    }

    };
    
}