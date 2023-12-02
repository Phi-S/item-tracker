using System.Diagnostics;
using application.Commands;
using infrastructure.Database;
using infrastructure.Database.Models;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
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
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        serviceCollection.AddScoped<PriceCommandService>();
        serviceCollection.AddScoped<ListCommandService>();
        await using var provider = serviceCollection.BuildServiceProvider();

        var dbContext = provider.GetRequiredService<XDbContext>();

        var list = await dbContext.ItemLists.AddAsync(new ItemListDbModel
        {
            UserId = "test_user",
            Name = "test_list",
            Url = "test_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        await dbContext.ItemListItemAction.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            PricePerOne = 1,
            Amount = 1,
            CreatedUtc = default
        });
        await dbContext.ItemListItemAction.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            PricePerOne = 1,
            Amount = 2,
            CreatedUtc = default
        });
        await dbContext.ItemListItemAction.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 1,
            Action = "S",
            PricePerOne = 1,
            Amount = 2,
            CreatedUtc = default
        });
        await dbContext.ItemListItemAction.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            PricePerOne = 1,
            Amount = 1,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var priceCommandService = provider.GetRequiredService<PriceCommandService>();
        var sw = Stopwatch.StartNew();
        var refreshItemPrices = await priceCommandService.RefreshItemPrices();
        if (refreshItemPrices.IsError)
        {
            Assert.Fail(refreshItemPrices.FirstError.Description);
        }
        sw.Stop();

        var itemPricesCount = dbContext.ItemPrices.Count();
        if (itemPricesCount <= 0)
        {
            Assert.Fail($"{itemPricesCount} item prices added to database");
        }

        _outputHelper.WriteLine($"{itemPricesCount} item prices added to database");

        var listValue = dbContext.ItemListValues.Where(listValue => listValue.List.Id == list.Entity.Id).ToList();
        if (listValue.Count == 0)
        {
            Assert.Fail("No itemListValues found");
        }

        if (listValue.Count != 1)
        {
            Assert.Fail($"Multiple list values found ({listValue.Count})");
        }

        _outputHelper.WriteLine($"priceCommandService.RefreshItemPrices duration: {sw.ElapsedMilliseconds}");
        Assert.True(true);
    }
}