using application.BackgroundServices;
using application.Cache;
using application.Commands;
using infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace application;

public static class ApplicationExtensionMethods
{
    public static IServiceCollection AddApplication(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddInfrastructure();
        serviceCollection.AddScoped<ListCommandService>();
        serviceCollection.AddScoped<PriceCommandService>();
        serviceCollection.AddScoped<ItemCommandService>();
        serviceCollection.AddSingleton<ListResponseCacheService>();
        serviceCollection.AddHostedService<RefreshPricesBackgroundService>();
        return serviceCollection;
    }
}