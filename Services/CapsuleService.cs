using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeCapsule.API.Data;
using System.Globalization;
using TimeCapsule.API.DTO;
using TimeCapsule.API.Entities;

namespace TimeCapsule.API.Services
{
    public class CapsuleService : ICapsuleService
    {
        private readonly double EarthRadiusKm = 6371.0;
        private readonly AppDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CapsuleService> _logger;
        public CapsuleService(AppDbContext context,IServiceScopeFactory scopeFactory,IHttpClientFactory httpClientFactory,ILogger<CapsuleService> logger)
        {
            _context = context;
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
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
                    await OpenCapsuleAsync(capsule,_context,autoSave: false);
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
   
        public async Task ProcessBackgroundTriggersAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var unopenedCapsules = await _context.TimeCapsules.Where(t=>t.IsOpened == false && t.Category != Entities.TriggerType.GeoFence).ToListAsync();
                    foreach (var capsule in unopenedCapsules)
                    {
                        switch (capsule.Category)
                        {
                            case Entities.TriggerType.Weather:

                                string apiUrl = "https://api.open-meteo.com/v1/forecast?latitude=38.48&longitude=28.14&current_weather=true";
                                var weatherData = await FetchExternalDataAsync<OpenMeteoResponse>(apiUrl);
                                var temp = weatherData?.current_weather.temperature;
                                if(temp!=null)
                                    {
                                        Console.WriteLine($"Hava Sıcaklığı:{temp}");
                                        double targetValueParsed = double.Parse(capsule.TargetValue);

                                        bool isMet = IsConditionMet(temp,targetValueParsed,capsule.Operator);

                                        if (isMet)
                                        {
                                            await OpenCapsuleAsync(capsule,_context);
                                        }

                                    }

                                break;
                            case Entities.TriggerType.Crypto:
                                    if (capsule.MetaData == null)
                                    {
                                     Console.WriteLine("Gerekli veriler tam değil!");
                                     break;   
                                    }
                                    var metaDataObj = System.Text.Json.JsonSerializer.Deserialize<CryptoMetaDataDTO>(capsule.MetaData);
                                    if (metaDataObj == null)
                                    {
                                     Console.WriteLine("Eksik veri girisi... Metadatayi doldurun");
                                     break;   
                                    }  
                                    string cryptoapiUrl = $"https://api.binance.com/api/v3/ticker/price?symbol={metaDataObj.symbol}";
                                    var cryptoData = await FetchExternalDataAsync<BinanceResponse>(cryptoapiUrl);
                                    if (cryptoData != null && cryptoData.price != null)
                                    {
                                        Console.WriteLine($"Güncel Fiyat:{cryptoData.price}");
                                        double targetPriceParsed = double.Parse(capsule.TargetValue);
                                        double currentPrice = double.Parse(cryptoData.price,CultureInfo.InvariantCulture);
                                        Console.WriteLine($"Güncel Fiyat Dönüştürülmüş:{currentPrice}");
                                        bool isMet = IsConditionMet(currentPrice,targetPriceParsed,capsule.Operator);
                                        if (isMet)
                                        {
                                            await OpenCapsuleAsync(capsule,_context);
                                        }
                                    }
                                break;
                            case Entities.TriggerType.Date:
                                DateTime now = DateTime.UtcNow;
                                if (!DateTime.TryParse(capsule.TargetValue,CultureInfo.InvariantCulture,DateTimeStyles.None,out DateTime targetDateParsed))
                                {
                                    Console.WriteLine($"{capsule.Id} numaralı kapsül için geçersiz tarih formatı...");
                                    continue;
                                }
                                if (now >= targetDateParsed)
                                {
                                    await OpenCapsuleAsync(capsule,_context);
                                }

                                break;
                            default:
                                Console.WriteLine("Bilinmeyen Kategori...");
                                break;
                        }
                    }
                }
        }
    
        public async Task CreateAndSaveCapsule(Entities.CapsuleEntity newCapsule)
        {
            _context.TimeCapsules.Add(newCapsule);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> TryUnlockSpotifyCapsuleAsync(int userId,string trackId)
        {
            var triggeredCapsule = await _context.TimeCapsules.FirstOrDefaultAsync(c=>c.UserID == userId && c.IsOpened == false && c.Category == TriggerType.SpotifyTrackId && c.TargetValue == trackId);

            if (triggeredCapsule == null)
            {
                return false;
            }
            await OpenCapsuleAsync(triggeredCapsule,_context);
            return true;
        }

        private async Task<T?> FetchExternalDataAsync<T>(string apiUrl)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await client.GetFromJsonAsync<T>(apiUrl);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception while waiting for a response... Message:{ex}");
                return default(T);
            }            
        }

        private bool IsConditionMet(double? currentValue,double targetValue,Entities.TriggerOperator? capsuleOperator)
        {
         return capsuleOperator switch
         {
             Entities.TriggerOperator.GreaterThan => currentValue > targetValue,
             Entities.TriggerOperator.LessThan => currentValue < targetValue,
             Entities.TriggerOperator.Equals => currentValue == targetValue,
             _ => false
         };
        }

        private async Task OpenCapsuleAsync(Entities.CapsuleEntity capsule, AppDbContext _context,bool autoSave = true)
        {
            capsule.IsOpened = true;
            if (autoSave)
            {
                await _context.SaveChangesAsync();
            }
            _logger.LogInformation($"{capsule.Id} numaralı kapsül başarıyla açıldıı. Hemen kontrol edin!!");
        }
    }
}