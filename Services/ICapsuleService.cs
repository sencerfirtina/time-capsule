using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TimeCapsule.API.Services
{
    public interface ICapsuleService
    {
        Task CreateAndSaveCapsule(Entities.CapsuleEntity newCapsule);
        Task<List<int>> CheckGeoFencesAsync(double userLat,double userLng);
        Task ProcessBackgroundTriggersAsync();
        Task<bool> TryUnlockSpotifyCapsuleAsync(int userId,string trackId);
    }
}