using infrastructure.ItemTrackerApi;
using Microsoft.Extensions.DependencyInjection;

namespace infrastructure;

public static class InfrastructureExtensionMethods
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<ItemTrackerApiService>();

        return services;
    }
}