using DbUp;
using infrastructure.Database;
using infrastructure.Database.Repos;
using infrastructure.ExchangeRates;
using infrastructure.ItemPriceFolder;
using infrastructure.Items;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Throw;

namespace infrastructure;

public static class InfrastructureExtensionMethods
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection serviceCollection)
    {
        #region Database

        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Migrate database
        using (var migrationServiceProvider = serviceCollection.BuildServiceProvider())
        {
            var loggerFactory = migrationServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("database migration");

            var configuration = migrationServiceProvider.GetRequiredService<IConfiguration>();
            var databaseConnectionString = configuration.GetValue<string>("DatabaseConnectionString");
            databaseConnectionString.ThrowIfNull().IfEmpty().IfWhiteSpace();

            serviceCollection.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(databaseConnectionString));
            EnsureDatabase.For.PostgresqlDatabase(databaseConnectionString);

            var upgrader =
                DeployChanges.To
                    .PostgresqlDatabase(databaseConnectionString)
                    .WithScriptsAndCodeEmbeddedInAssembly(typeof(ItemsService).Assembly)
                    .LogTo(logger)
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (result.Successful == false)
            {
                throw new Exception($"failed to upgrade database. {result.Error}");
            }

            logger.LogInformation("upgrading database successfully");
        }

        #endregion

        serviceCollection.AddHttpClient();
        serviceCollection.AddSingleton<ItemsService>();
        serviceCollection.AddScoped<ItemPriceService>();
        serviceCollection.AddScoped<ExchangeRatesService>();
        return serviceCollection;
    }
}