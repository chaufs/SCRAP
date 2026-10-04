using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SCRAP.infrastructure.services;

namespace SCRAP.API.Services
{
    public class CloudSyncBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<CloudSyncBackgroundWorker> _logger;

        public CloudSyncBackgroundWorker(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<CloudSyncBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool isEnabled = _config.GetValue<bool>("CloudSync:Enabled", true);
            if (!isEnabled)
            {
                _logger.LogInformation("Cloud sync background worker is disabled in configuration.");
                return;
            }

            int intervalSeconds = _config.GetValue<int>("CloudSync:AutoSyncIntervalSeconds", 60);
            if (intervalSeconds < 15) intervalSeconds = 15;

            _logger.LogInformation("Cloud sync background worker started. Auto-sync interval: {Interval} seconds.", intervalSeconds);

            // Initial startup delay to allow API and seeding to complete
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var syncService = scope.ServiceProvider.GetRequiredService<ICloudSyncService>();
                        var status = await syncService.GetSyncStatusAsync();

                        if (status.IsOnline)
                        {
                            _logger.LogInformation("Cloud connection detected. Executing background cloud sync...");
                            var result = await syncService.SyncAllAsync();
                            if (result.Success)
                            {
                                _logger.LogInformation("Cloud sync completed successfully. Total records synced: {TotalSynced}", result.RecordsPushed);
                            }
                            else
                            {
                                _logger.LogWarning("Cloud sync completed with warnings/errors: {Errors}", string.Join("; ", result.Details));
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Cloud is offline. Operating in local mode.");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in CloudSyncBackgroundWorker loop.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Cloud sync background worker stopped.");
        }
    }
}
