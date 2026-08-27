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
        private readonly ILogger<CapsuleMonitorService> _logger;
        public CapsuleMonitorService(IServiceScopeFactory scopeFactory,ILogger<CapsuleMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var capsuleService = scope.ServiceProvider.GetRequiredService<ICapsuleService>();
                        await capsuleService.ProcessBackgroundTriggersAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,"There was an error while the background processer triggers running");    
                }
                
                await Task.Delay(TimeSpan.FromSeconds(8),stoppingToken);
                
            }
        }
    }
}