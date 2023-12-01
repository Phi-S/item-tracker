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
}