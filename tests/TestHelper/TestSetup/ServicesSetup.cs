using application;
using infrastructure;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DockerContainerFolder;
using TestHelper.TestConfigurationFolder;
using TestHelper.TestLoggerFolder;
using Xunit.Abstractions;

namespace TestHelper.TestSetup;

public static class ServicesSetup
{
    public static async Task<IServiceCollection> GetApiInfrastructureCollection(ITestOutputHelper outputHelper)
    {
        var (id, containerName, connectionString) = await PostgresContainer.StartNew(outputHelper);
        var config = TestConfiguration.GetApiAppSettingsTest("DatabaseConnectionString", connectionString);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTestLogger(outputHelper);
        serviceCollection.AddSingleton(config);
        serviceCollection.AddInfrastructure();
        return serviceCollection;
    }

    public static async Task<IServiceCollection> GetApiApplicationCollection(
        ITestOutputHelper outputHelper,
        bool disableListResponseCacheService = true,
        bool disableRefreshPricesBackgroundService = true)
    {
        var (id, containerName, connectionString) = await PostgresContainer.StartNew(outputHelper);
        var config = TestConfiguration.GetApiAppSettingsTest(
            [
                new KeyValuePair<string, string?>("DatabaseConnectionString", connectionString),
                new KeyValuePair<string, string?>("DisableCache", disableListResponseCacheService.ToString()),
                new KeyValuePair<string, string?>("DisableRefreshPricesBackgroundService",
                    disableRefreshPricesBackgroundService.ToString())
            ]
        );
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTestLogger(outputHelper);
        serviceCollection.AddSingleton(config);
        serviceCollection.AddApplication();
        return serviceCollection;
    }
}