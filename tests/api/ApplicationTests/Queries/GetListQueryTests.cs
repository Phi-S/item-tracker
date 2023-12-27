using application.Queries;
using infrastructure.Database;
using infrastructure.Database.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.RandomHelperFolder;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Queries;

public class GetListQueryTests
{
    private readonly ITestOutputHelper _outputHelper;

    public GetListQueryTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task SGetListQueryTest_Ok()
    {
        // Arrange
        var serviceCollection = await ServicesSetup.GetApiApplicationCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();

        var userId = RandomHelper.RandomString();
        var listUrl = RandomHelper.RandomString();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            UserId = userId,
            Name = RandomHelper.RandomString(),
            Description = null,
            Url = listUrl,
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
            Amount = 2,
            CreatedUtc = DateTime.UtcNow
        });

        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 1,
            Action = "S",
            UnitPrice = 2,
            Amount = 2,
            CreatedUtc = DateTime.UtcNow.AddSeconds(1)
        });

        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 2,
            Action = "B",
            UnitPrice = 3,
            Amount = 4,
            CreatedUtc = DateTime.UtcNow.AddSeconds(1)
        });

        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 2,
            Action = "B",
            UnitPrice = 4,
            Amount = 5,
            CreatedUtc = DateTime.UtcNow.AddSeconds(2)
        });

        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 2,
            Action = "S",
            UnitPrice = 1,
            Amount = 2,
            CreatedUtc = DateTime.UtcNow.AddSeconds(5)
        });

        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            List = list.Entity,
            ItemId = 3,
            Action = "B",
            UnitPrice = 1,
            Amount = 2,
            CreatedUtc = default
        });

        var priceRefresh = await dbContext.PricesRefresh.AddAsync(new ItemPriceRefreshDbModel
        {
            UsdToEurExchangeRate = 2,
            SteamPricesLastModified = default,
            Buff163PricesLastModified = default,
            CreatedUtc = default
        });

        await dbContext.Prices.AddAsync(new ItemPriceDbModel
        {
            ItemId = 1,
            SteamPriceCentsUsd = 1,
            Buff163PriceCentsUsd = 2,
            ItemPriceRefresh = priceRefresh.Entity
        });

        await dbContext.Prices.AddAsync(new ItemPriceDbModel
        {
            ItemId = 2,
            SteamPriceCentsUsd = 3,
            Buff163PriceCentsUsd = 4,
            ItemPriceRefresh = priceRefresh.Entity
        });

        await dbContext.Prices.AddAsync(new ItemPriceDbModel
        {
            ItemId = 3,
            SteamPriceCentsUsd = 5,
            Buff163PriceCentsUsd = 6,
            ItemPriceRefresh = priceRefresh.Entity
        });

        await dbContext.SaveChangesAsync();

        // Act
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new GetListQuery(userId, listUrl);
        var result = await mediator.Send(command);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }

        _outputHelper.WriteLine(result.Value.ToString());
        var listResponse = result.Value;
        Assert.Equal(9, listResponse.ItemCount);
        Assert.Equal(27, listResponse.InvestedCapital);
        Assert.Equal(62, result.Value.SteamSellPrice);
        Assert.Equal(80, result.Value.Buff163SellPrice);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.Contains(listResponse.Items, item => item.ItemId == 1);
        Assert.Contains(listResponse.Items, item => item.ItemId == 2);
        Assert.Contains(listResponse.Items, item => item.ItemId == 3);
    }
}