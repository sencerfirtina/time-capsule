using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using TimeCapsule.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using TimeCapsule.API.DTO;
using System.Globalization;

namespace TimeCapsule.API.Services
{
    public class CapsuleMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        public CapsuleMonitorService(IServiceScopeFactory scopeFactory,IHttpClientFactory httpclientFactory)
        {
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpclientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var unopenedCapsules = await _context.TimeCapsules.Where(t=>t.IsOpened == false).ToListAsync();
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
                            default:
                                Console.WriteLine("Bilinmeyen Kategori...");
                                break;
                        }
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(8),stoppingToken);
            }
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

        private bool IsConditionMet(double? currentValue,double targetValue,Entities.TriggerOperator capsuleOperator)
        {
         return capsuleOperator switch
         {
             Entities.TriggerOperator.GreaterThan => currentValue > targetValue,
             Entities.TriggerOperator.LessThan => currentValue < targetValue,
             Entities.TriggerOperator.Equals => currentValue == targetValue,
             _ => false
         };
        }   
    
        private async Task OpenCapsuleAsync(Entities.CapsuleEntity capsule, AppDbContext _context)
        {
            capsule.IsOpened = true;
            await _context.SaveChangesAsync();
            Console.WriteLine($"{capsule.Id} numaralı kapsül başarıyla açıldıı. Hemen kontrol edin!!");
        }
    }
}