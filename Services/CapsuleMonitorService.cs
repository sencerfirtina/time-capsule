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
                                var client = _httpClientFactory.CreateClient();
                                try{
                                    var weatherData = await client.GetFromJsonAsync<OpenMeteoResponse>(apiUrl);
                                    var temp = weatherData?.current_weather.temperature;
                                    if(temp!=null)
                                    {
                                        Console.WriteLine($"Hava Sıcaklığı:{temp}");
                                        double targetValueParsed = double.Parse(capsule.TargetValue);

                                        bool isConditionMet = capsule.Operator switch
                                        {
                                            Entities.TriggerOperator.GreaterThan => temp > targetValueParsed,
                                            
                                            Entities.TriggerOperator.LessThan => temp < targetValueParsed,
                                            
                                            Entities.TriggerOperator.Equals => temp == targetValueParsed,

                                            _ => false
                                        };

                                        if (isConditionMet)
                                        {
                                            capsule.IsOpened = true;
                                            await _context.SaveChangesAsync();
                                            Console.WriteLine($"{capsule.Id} numaralı kapsül başarıyla açıldı hemen göz atın!!");
                                        }

                                    }
                                }
                                catch(Exception ex)
                                {
                                    Console.WriteLine($"Api yolda kaldı... Sebep:{ex}");
                                }


                                break;
                            case Entities.TriggerType.Crypto:
                                Console.WriteLine("Crypto abeeee");
                                try
                                {
                                    if (capsule.MetaData == null)
                                    {
                                     Console.WriteLine("Gerekli veriler tam değil!");
                                     break;   
                                    }
                                    //bir de kodun şu kısımlarının aşırı tekrar etmesi var bunları gerekirse fonksiyona alma kısmında kaldım
                                    var metaDataObj = System.Text.Json.JsonSerializer.Deserialize<CryptoMetaDataDTO>(capsule.MetaData);  
                                    string cryptoapiUrl = $"https://api.binance.com/api/v3/ticker/price?symbol={metaDataObj.symbol}";
                                    var cryptoClient = _httpClientFactory.CreateClient();
                                    var cryptoData = await cryptoClient.GetFromJsonAsync<BinanceResponse>(cryptoapiUrl);
                                    if (cryptoData != null && cryptoData.price != null)
                                    {
                                        Console.WriteLine($"Güncel Fiyat:{cryptoData.price}");
                                        double targetPriceParsed = double.Parse(capsule.TargetValue);
                                        //şu dönüşümde noktayı kaybetmeme kısmını düzeltmekte kaldım
                                        double currentPrice = double.Parse(cryptoData.price);
                                        Console.WriteLine($"{currentPrice}");
                                        bool conditionMet = capsule.Operator switch
                                        {
                                            Entities.TriggerOperator.GreaterThan => currentPrice > targetPriceParsed,
                                            Entities.TriggerOperator.LessThan => currentPrice < targetPriceParsed,
                                            Entities.TriggerOperator.Equals => currentPrice == targetPriceParsed,
                                            _ => false
                                        };
                                        if (conditionMet)
                                        {
                                            capsule.IsOpened = true;
                                            await _context.SaveChangesAsync();
                                            Console.WriteLine($"{capsule.Id} numaralı kapsül başarıyla açıldı hemen göz atın!!"); 
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Eyvah crypto kuryesi yolda kaldı... Hata:{ex}");
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
    }
}