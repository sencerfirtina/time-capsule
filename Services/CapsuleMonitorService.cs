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
                                Console.WriteLine("Hava durumu abeeee");

                                string apiUrl = "https://api.open-meteo.com/v1/forecast?latitude=38.48&longitude=28.14&current_weather=true";
                                var client = _httpClientFactory.CreateClient();
                                try{
                                    var weatherData = await client.GetFromJsonAsync<OpenMeteoResponse>(apiUrl);
                                    var temp = weatherData?.current_weather.temperature;
                                    if(temp!=null)
                                    {
                                        Console.WriteLine($"Hava Sıcaklığı:{temp}");
                                    }
                                }
                                catch(Exception ex)
                                {
                                    Console.WriteLine($"Api yolda kaldı... Sebep:{ex}");
                                }


                                break;
                            case Entities.TriggerType.Crypto:
                                Console.WriteLine("Crypto abeeee");
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