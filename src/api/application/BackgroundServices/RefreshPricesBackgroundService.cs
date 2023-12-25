using application.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace application.BackgroundServices;

public class RefreshPricesBackgroundService : BackgroundService
{
    private readonly ILogger<RefreshPricesBackgroundService> _logger;
    private readonly IServiceProvider _services;

    public RefreshPricesBackgroundService(
        ILogger<RefreshPricesBackgroundService> logger,
        IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int executionHour = 20;
        const int sleepBetweenChecksInSec = 30;

        _logger.LogInformation("RefreshPricesBackgroundService started");
        var retries = 0;
        var executed = false;
        while (stoppingToken.IsCancellationRequested == false)
        {
            await Task.Delay(TimeSpan.FromSeconds(sleepBetweenChecksInSec), stoppingToken);

            var currentDateTimeUtcHour = DateTime.UtcNow.Hour;
            if (currentDateTimeUtcHour is not executionHour)
            {
                executed = false;
                continue;
            }

            if (executed)
            {
                continue;
            }

            _logger.LogInformation("Refreshing item prices");
            using (var scope = _services.CreateScope())
            {
                var priceCommandService = scope.ServiceProvider.GetRequiredService<PriceCommandService>();
                var refreshItemPrices = await priceCommandService.RefreshItemPrices();
                if (refreshItemPrices.IsError)
                {
                    retries++;
                    if (retries == 3)
                    {
                        _logger.LogWarning("Failed to refresh item prices 3 times. Not Retrying");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to refresh item prices. Retrying in 5 Minutes");
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        continue;
                    }
                }
            }

            retries = 0;
            executed = true;
        }
        _logger.LogWarning("RefreshPricesBackgroundService exited");
    }
}