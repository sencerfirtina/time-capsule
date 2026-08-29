using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeCapsule.API.Entities;

namespace TimeCapsule.API.Data.Repositories
{
    public class CapsuleRepository : ICapsuleRepository
    {
        private readonly AppDbContext _context;
        public CapsuleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddCapsuleAsync(CapsuleEntity capsule)
        {
            await _context.TimeCapsules.AddAsync(capsule);
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<CapsuleEntity>> GetPendingCapsulesByCategoryAsync(int userId,TriggerType category)
        {
            return await _context.TimeCapsules.Where(c=>c.UserID == userId && c.IsOpened == false && c.Category == category).ToListAsync();
        }

        public async Task<List<CapsuleEntity>> GetPendingCapsulesForBackroundAsync(IEnumerable<TriggerType> categories)
        {
            return await _context.TimeCapsules.Where(c=>c.IsOpened == false && categories.Contains(c.Category)).ToListAsync();
        }

        public async Task<List<CapsuleEntity>> GetPendingSpotifyCapsulesAsync(int userId, string trackId)
        {
            return await _context.TimeCapsules.Where(c=>c.IsOpened == false && c.Category == TriggerType.SpotifyTrackId && c.TargetValue == trackId).ToListAsync();
        }
    }
}