using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TimeCapsule.API.Services
{
    public interface ICapsuleService
    {
        Task<List<int>> CheckGeoFencesAsync(double userLat,double userLng);
    }
}