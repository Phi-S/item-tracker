using application.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace application;

public static class ApplicationExtensionMethods
{
    public static IServiceCollection AddApplication(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ListCommandService>();
        serviceCollection.AddScoped<PriceCommandService>();
        return serviceCollection;
    }
}