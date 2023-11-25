using infrastructure.ItemTrackerApi;
using Microsoft.Extensions.DependencyInjection;

namespace infrastructure;

public static class InfrastructureExtensionMethods
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string itemTrackerRApiEndpoint)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(itemTrackerRApiEndpoint)
        };
        services.AddKeyedSingleton(nameof(ItemTrackerApiService), httpClient);
        services.AddSingleton<ItemTrackerApiService>();

        return services;
    }
}