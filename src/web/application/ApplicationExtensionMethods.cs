using Microsoft.Extensions.DependencyInjection;

namespace application;

public static class ApplicationExtensionMethods
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}