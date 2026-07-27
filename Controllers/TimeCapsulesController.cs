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
     public TimeCapsulesController(ICapsuleService capsuleService)
        {
            _capsuleService = capsuleService;
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
            await _capsuleService.CreateAndSaveCapsule(newEntity);
            return Ok("Kapsül başarıyla gömüldü");
        }

    [HttpPost("check-location")]
    public async Task<IActionResult> CheckLocationTriggers([FromBody] LocationCheckRequestDTO userLocation)
    {
        var openedCapsuleIds = await _capsuleService.CheckGeoFencesAsync(userLocation.Latitude,userLocation.Longitude);
        return Ok(new {Message = $"{openedCapsuleIds.Count()} adet kapsül açıldı!",OpenedIds = openedCapsuleIds});
    }
    };
    
}