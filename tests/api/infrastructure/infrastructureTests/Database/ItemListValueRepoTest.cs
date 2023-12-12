using System.Diagnostics;
using infrastructure.Database;
using infrastructure.Database.Models;
using infrastructure.Database.Repos;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace infrastructureTests.Database;

public class ItemListValueRepoTest
{
    private readonly ITestOutputHelper _outputHelper;

    public ItemListValueRepoTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task CalculateLatestTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
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
        
        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            UnitPrice = 1,
            Amount = 1,
            CreatedUtc = default
        });
        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            UnitPrice = 1,
            Amount = 2,
            CreatedUtc = default
        });
        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 1,
            Action = "S",
            UnitPrice = 1,
            Amount = 2,
            CreatedUtc = default
        });
        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            UnitPrice = 1,
            Amount = 1,
            CreatedUtc = default
        });

        var itemPriceRefresh = await dbContext.PricesRefresh.AddAsync(
            new ItemPriceRefreshDbModel
            {
                SteamPricesLastModified = default,
                Buff163PricesLastModified = default,
                CreatedUtc = default,
                UsdToEurExchangeRate = 1
            });
        await dbContext.Prices.AddAsync(new ItemPriceDbModel
        {
            ItemId = 1,
            SteamPriceCentsUsd = 2,
            Buff163PriceCentsUsd = 4,
            ItemPriceRefresh = itemPriceRefresh.Entity
        });

        await dbContext.Prices.AddAsync(new ItemPriceDbModel
        {
            ItemId = 1,
            ItemPriceRefresh = itemPriceRefresh.Entity
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListValueRepo = unitOfWork.ItemListSnapshotRepo;
        var sw = Stopwatch.StartNew();
        var newItemListValue = await itemListValueRepo.CalculateWithLatestPrices(list.Entity);
        await unitOfWork.Save();
        _outputHelper.WriteLine($"itemListValueRepo.CalculateLatest duration: {sw.ElapsedMilliseconds} ms");
        Assert.True(newItemListValue.SteamValue.HasValue);
        Assert.True(newItemListValue.SteamValue.Value == 4);
        Assert.True(newItemListValue.BuffValue.HasValue);
        Assert.True(newItemListValue.BuffValue.Value == 8);
    }
}