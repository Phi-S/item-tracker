using application.Commands;
using infrastructure;
using infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.DockerContainerFolder;
using TestHelper.TestConfigurationFolder;
using TestHelper.TestLoggerFolder;
using Xunit.Abstractions;

namespace ApplicationTests;

public class PriceCommandServiceTests
{
    private readonly ITestOutputHelper _outputHelper;

    public PriceCommandServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task RefreshItemPricesTest()
    {
        var (id, containerName, connectionString) = await PostgresContainer.StartNew(_outputHelper);
        var config = TestConfiguration.GetApiAppSettingsTest("DatabaseConnectionString", connectionString);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTestLogger(_outputHelper);
        serviceCollection.AddSingleton(config);
        serviceCollection.AddInfrastructure();
        serviceCollection.AddScoped<PriceCommandService>();
        await using var provider = serviceCollection.BuildServiceProvider();
        var priceCommandService = provider.GetRequiredService<PriceCommandService>();
        var refreshItemPrices = await priceCommandService.RefreshItemPrices();
        if (refreshItemPrices.IsError)
        {
            Assert.Fail(refreshItemPrices.FirstError.Description);
        }

        var dbContext = provider.GetRequiredService<XDbContext>();
        var itemPricesCount = dbContext.ItemPrices.Count();
        if (itemPricesCount <= 0)
        {
            Assert.Fail($"{itemPricesCount} item prices added to database");
        }

        _outputHelper.WriteLine($"{itemPricesCount} item prices added to database");
        Assert.True(true);
    }
}