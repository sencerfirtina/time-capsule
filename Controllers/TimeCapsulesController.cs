using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCapsule.API.Entities;
using TimeCapsule.API.Data;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TimeCapsule.API.DTO;
using TimeCapsule.API.Services;


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

    [HttpPost]
    public async Task<IActionResult> CreateCapsule([FromBody] CreateCapsuleDTO capsuleDTO)
        {
            var newEntity = new Entities.CapsuleEntity
            {
                EncryptedContent = capsuleDTO.EncryptedContent,
                Category = capsuleDTO.Category,
                Operator = capsuleDTO.Operator,
                TargetValue = capsuleDTO.TargetValue,
                MetaData = capsuleDTO.MetaData
            };
            await _capsuleService.CreateAndSaveCapsule(1,newEntity);
            return Ok("Kapsül başarıyla gömüldü");
        }

    [HttpPost("check-location")]
    public async Task<IActionResult> CheckLocationTriggers([FromBody] LocationCheckRequestDTO userLocation)
    {
        var openedCapsuleIds = await _capsuleService.CheckGeoFencesAsync(1,userLocation.Latitude,userLocation.Longitude);
        return Ok(new {Message = $"{openedCapsuleIds.Count()} adet kapsül açıldı!",OpenedIds = openedCapsuleIds});
    }

    [HttpGet("check-spotify-trigger")]
    public async Task<IActionResult> CheckSpotifyTrigger()
    {
        var currentTrack = await _spotifyService.GetCurrentlyPlayingAsync(1);

        if (!currentTrack.isSuccess)
        {
            return BadRequest(currentTrack.ErrorMessage);       
        }

        var response = await _capsuleService.TryUnlockSpotifyCapsuleAsync(1,currentTrack.TrackId!);
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