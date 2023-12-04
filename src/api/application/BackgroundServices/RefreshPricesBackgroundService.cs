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
        _logger.LogInformation("RefreshPricesBackgroundService started");
        var executedAt8 = DateTime.UtcNow.Hour == 8;
        var executedAt20 = DateTime.UtcNow.Hour == 20;
        var retries = 0;
        while (stoppingToken.IsCancellationRequested == false)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            var currentDateTimeUtcHour = DateTime.UtcNow.Hour;
            if (currentDateTimeUtcHour is not (8 or 20))
            {
                continue;
            }

            if (currentDateTimeUtcHour is 8 && executedAt8)
            {
                continue;
            }

            if (currentDateTimeUtcHour is 20 && executedAt20)
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
                else
                {
                    _logger.LogInformation("Item prices refreshed");
                }
            }

            retries = 0;
            if (currentDateTimeUtcHour is 8)
            {
                executedAt8 = true;
                executedAt20 = false;
            }
            else
            {
                executedAt20 = true;
                executedAt8 = true;
            }
        }
    }
}