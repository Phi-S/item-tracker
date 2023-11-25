using infrastructure.Database;
using infrastructure.Database.Repos;
using infrastructure.ExchangeRates;
using infrastructure.ItemPriceFolder;
using infrastructure.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace infrastructure;

public static class InfrastructureExtensionMethods
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection serviceCollection)
    {
        #region Database
        serviceCollection.AddDbContext<XDbContext>();

        // Migrate database
        using (var migrationServiceProvider = serviceCollection.BuildServiceProvider())
        {
            using (var dbContext = migrationServiceProvider.GetRequiredService<XDbContext>())
            {
                dbContext.Database.Migrate();
            }
        }

        serviceCollection.AddScoped<ItemListRepo>();
        serviceCollection.AddScoped<ItemListItemRepo>();
        serviceCollection.AddScoped<ItemListValueRepo>();
        serviceCollection.AddScoped<ItemPriceRepo>();

        #endregion

        serviceCollection.AddHttpClient();
        serviceCollection.AddSingleton<ItemsService>();
        serviceCollection.AddScoped<ItemPriceService>();
        serviceCollection.AddScoped<ExchangeRatesService>();
        return serviceCollection;
    }
}