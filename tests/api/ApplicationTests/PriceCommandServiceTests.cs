using application.Commands;
using infrastructure;
using infrastructure.Database;
using infrastructure.Database.Models;
using infrastructure.Database.Repos;
using Microsoft.Extensions.DependencyInjection;
using shared.Models;
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
        serviceCollection.AddScoped<ListCommandService>();
        await using var provider = serviceCollection.BuildServiceProvider();

        var userId = "test_user_id";
        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
        var newList = await itemListRepo.New(userId, "test_list_name", "test_list_desc", "EUR", false);

        var itemListItemRepo = provider.GetRequiredService<ItemListItemRepo>();
        await itemListItemRepo.Buy(newList, 7, 1, 9);
        await itemListItemRepo.Buy(newList, 7, 2, 8);
        await itemListItemRepo.Buy(newList, 7, 3, 7);
        await itemListItemRepo.Sell(newList, 7, 4, 6);
        await itemListItemRepo.Buy(newList, 7, 5, 5);
        await itemListItemRepo.Buy(newList, 7, 6, 4);
        await itemListItemRepo.Buy(newList, 7, 7, 3);
        await itemListItemRepo.Buy(newList, 7, 8, 2);
        await itemListItemRepo.Buy(newList, 16, 9, 1);

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

        var itemListValueRepo = provider.GetRequiredService<ItemListValueRepo>();
        var itemListValues = await itemListValueRepo.GetAll(newList);
        if (itemListValues.Count == 0)
        {
            Assert.Fail("No itemListValues found");
        }

        if (itemListValues.Count != 1)
        {
            Assert.Fail($"Multiple list values found ({itemListValues.Count})");
        }

        _outputHelper.WriteLine($"List value: {itemListValues.First()}");
        Assert.True(true);
    }

    [Fact]
    public async Task GetTotalItemsValueTest()
    {
        var items = new List<ItemListItemActionDbModel>()
        {
            new()
            {
                Action = "B",
                ItemId = 1,
                PricePerOne = 0,
                Amount = 1,
                CreatedUtc = DateTime.UtcNow,
                ItemListDbModel = null
            },
            new()
            {
                Action = "B",
                ItemId = 1,
                PricePerOne = 0,
                Amount = 1,
                CreatedUtc = DateTime.UtcNow,
                ItemListDbModel = null
            },
            new()
            {
                Action = "B",
                ItemId = 1,
                PricePerOne = 0,
                Amount = 1,
                CreatedUtc = DateTime.UtcNow,
                ItemListDbModel = null
            },
            new()
            {
                Action = "B",
                ItemId = 1,
                PricePerOne = 0,
                Amount = 3,
                CreatedUtc = DateTime.UtcNow,
                ItemListDbModel = null
            },
            new()
            {
                Action = "S",
                ItemId = 1,
                PricePerOne = 0,
                Amount = 5,
                CreatedUtc = DateTime.UtcNow,
                ItemListDbModel = null
            },
            new()
            {
                Action = "B",
                ItemId = 1,
                PricePerOne = 0,
                Amount = 5,
                CreatedUtc = DateTime.UtcNow,
                ItemListDbModel = null
            },
            new()
            {
                Action = "S",
                ItemId = 1,
                PricePerOne = 0,
                Amount = 5,
                CreatedUtc = DateTime.UtcNow,
                ItemListDbModel = null
            }
        };
        var prices = new List<ItemPriceDbModel>()
        {
            new()
            {
                ItemId = 1,
                SteamPriceUsd = 2,
                SteamPriceEur = 4,
                BuffPriceUsd = 1,
                BuffPriceEur = 2,
                CreatedUtc = DateTime.UtcNow
            }
        };
        var totalItemsValueEur = PriceCommandService.GetTotalItemsValue(items, prices, "EUR");
        if (totalItemsValueEur.IsError)
        {
            Assert.Fail(totalItemsValueEur.FirstError.Description);
        }

        var totalItemsValueUsd = PriceCommandService.GetTotalItemsValue(items, prices, "USD");
        if (totalItemsValueUsd.IsError)
        {
            Assert.Fail(totalItemsValueUsd.FirstError.Description);
        }

        Assert.True(totalItemsValueEur.Value is { steamValue: 4, buffValue: 2 } &&
                    totalItemsValueUsd.Value is { steamValue: 2, buffValue: 1 });
    }
}