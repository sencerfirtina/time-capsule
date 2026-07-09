using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCapsule.API.Entities;
using TimeCapsule.API.Data;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TimeCapsule.API.DTO;


namespace TimeCapsule.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeCapsulesController : ControllerBase
    {
     private readonly AppDbContext _context;
     public TimeCapsulesController(AppDbContext context)
        {
            _context = context;
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
            _context.TimeCapsules.Add(newEntity);
            await _context.SaveChangesAsync();

            return Ok("Kapsül başarıyla gömüldü");
        }
    };
}