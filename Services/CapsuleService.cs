using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeCapsule.API.Data;
using System.Globalization;

namespace TimeCapsule.API.Services
{
    public class CapsuleService : ICapsuleService
    {
        private readonly double EarthRadiusKm = 6371.0;
        private readonly AppDbContext _context;
        public CapsuleService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<int>> CheckGeoFencesAsync(double userLat,double userLng)
        {
            var openedCapsules = new List<int>();
            var pendingCapsules = await _context.TimeCapsules.Where(c=>c.IsOpened == false && c.Category == Entities.TriggerType.GeoFence).ToListAsync();
            foreach (var capsule in pendingCapsules)
            {
                string[] coordinates = capsule.TargetValue.Split(',');
                double.TryParse(coordinates[0],CultureInfo.InvariantCulture, out double targetLat);
                double.TryParse(coordinates[1],CultureInfo.InvariantCulture, out double targetLng);
                if (coordinates.Count() != 2 || targetLat == 0 || targetLng == 0)
                {
                    Console.WriteLine("Koordinat formatı düzgün girilmemiş kapsül geçiliyor...");
                    continue;
                }
                var distance = CalculateDistance(userLat,userLng,targetLat,targetLng);
                if (distance <= 100)
                {
                    capsule.IsOpened = true;
                    openedCapsules.Add(capsule.Id);
                }
            }
            await _context.SaveChangesAsync();
            return openedCapsules;
        }

        private double CalculateDistance(double lat1,double lon1, double lat2, double lon2)
        {
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            lat1 = ToRadians(lat1);
            lat2 = ToRadians(lat2);

            double a = Math.Pow(Math.Sin(dLat / 2), 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Pow(Math.Sin(dLon / 2), 2);
            
            double c = 2 * Math.Asin(Math.Sqrt(a));

            return EarthRadiusKm * c * 1000;
        }
        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }
    }
}