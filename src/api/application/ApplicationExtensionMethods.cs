using application.BackgroundServices;
using application.Cache;
using infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace application;

public static class ApplicationExtensionMethods
{
    public static IServiceCollection AddApplication(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddInfrastructure();
        serviceCollection.AddSingleton<ListResponseCacheService>();
        serviceCollection.AddHostedService<RefreshPricesBackgroundService>();
        serviceCollection.AddApplicationMediatR();
        return serviceCollection;
    }

    public static IServiceCollection AddApplicationMediatR(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationExtensionMethods).Assembly));
    }
}