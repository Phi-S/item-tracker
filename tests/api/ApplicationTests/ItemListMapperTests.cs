using application.Mapper;
using infrastructure.Database.Models;
using infrastructure.Items;
using shared.Currencies;
using Xunit.Abstractions;

namespace ApplicationTests;

public class ItemListMapperTest
{
    private readonly ITestOutputHelper _outputHelper;

    public ItemListMapperTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void MapListItemResponseTest_Ok()
    {
        // Arrange
        var item = new ItemModel(1, "1", "item_1", "item_1_hash", "item_1_url", "item_1_image");

        var list = new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "name",
            Description = null,
            Url = "url",
            Currency = CurrenciesConstants.EURO,
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        };
        var priceRefresh = new ItemPriceRefreshDbModel
        {
            Id = 1,
            UsdToEurExchangeRate = 2,
            SteamPricesLastModified = default,
            Buff163PricesLastModified = default,
            CreatedUtc = default
        };

        var prices = new List<ItemPriceDbModel>()
        {
            new ItemPriceDbModel
            {
                Id = 1,
                ItemId = item.Id,
                SteamPriceCentsUsd = 1,
                Buff163PriceCentsUsd = 2,
                ItemPriceRefresh = priceRefresh
            }
        };
        var itemAction = new List<ItemListItemActionDbModel>
        {
            new()
            {
                Id = 1,
                List = list,
                ItemId = item.Id,
                Action = "B",
                UnitPrice = 1,
                Amount = 1,
                CreatedUtc = default
            },
            new()
            {
                Id = 2,
                List = list,
                ItemId = item.Id,
                Action = "B",
                UnitPrice = 44,
                Amount = 1,
                CreatedUtc = default
            },
            new()
            {
                Id = 3,
                List = list,
                ItemId = item.Id,
                Action = "S",
                UnitPrice = 100,
                Amount = 2,
                CreatedUtc = default
            },
            new()
            {
                Id = 4,
                List = list,
                ItemId = item.Id,
                Action = "B",
                UnitPrice = 3,
                Amount = 11,
                CreatedUtc = default
            }
        };

        // Act
        var listItemResponse = ItemListMapper.MapListItemResponse(
            list,
            item,
            itemAction,
            priceRefresh,
            prices
        );

        // Assert
        Assert.Equal(item.Id, listItemResponse.ItemId);
        Assert.Equal(item.Name, listItemResponse.ItemName);
        Assert.Equal(item.Image, listItemResponse.ItemImage);
        Assert.Equal(33, listItemResponse.InvestedCapital);
        Assert.Equal(11, listItemResponse.ItemCount);
        Assert.Equal(3, listItemResponse.AverageBuyPriceForOne);
        Assert.Equal(200, listItemResponse.SalesValue);
        Assert.Equal(156, listItemResponse.Profit);
        Assert.Equal(2, listItemResponse.SteamSellPriceForOne);
        Assert.Equal(4, listItemResponse.Buff163SellPriceForOne);
        Assert.Equal(itemAction.Count, listItemResponse.Actions.Count);
        _outputHelper.WriteLine(listItemResponse.ToString());
    }

    [Fact]
    public void MapListSnapshotResponse_Ok()
    {
        // Arrange
        long itemId1 = 1;
        long itemId2 = 2;
        var list = new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "name",
            Description = null,
            Url = "url",
            Currency = CurrenciesConstants.EURO,
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        };
        var priceRefresh = new ItemPriceRefreshDbModel
        {
            Id = 1,
            UsdToEurExchangeRate = 2,
            SteamPricesLastModified = default,
            Buff163PricesLastModified = default,
            CreatedUtc = default
        };
        var prices = new List<ItemPriceDbModel>()
        {
            new()
            {
                Id = 1,
                ItemId = itemId1,
                SteamPriceCentsUsd = 1,
                Buff163PriceCentsUsd = 2,
                ItemPriceRefresh = priceRefresh
            },
            new()
            {
                Id = 2,
                ItemId = itemId2,
                SteamPriceCentsUsd = 3,
                Buff163PriceCentsUsd = 4,
                ItemPriceRefresh = priceRefresh
            }
        };
        var snapshot = new ItemListSnapshotDbModel
        {
            Id = 1,
            List = list,
            ItemPriceRefresh = priceRefresh,
            CreatedUtc = DateTime.UtcNow.AddMinutes(30)
        };

        var itemAction = new List<ItemListItemActionDbModel>
        {
            new()
            {
                Id = 1,
                List = list,
                ItemId = itemId1,
                Action = "B",
                UnitPrice = 2,
                Amount = 1,
                CreatedUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                List = list,
                ItemId = itemId1,
                Action = "B",
                UnitPrice = 44,
                Amount = 1,
                CreatedUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                List = list,
                ItemId = itemId1,
                Action = "S",
                UnitPrice = 100,
                Amount = 2,
                CreatedUtc = DateTime.UtcNow
            },
            new()
            {
                Id = 4,
                List = list,
                ItemId = itemId1,
                Action = "B",
                UnitPrice = 3,
                Amount = 11,
                CreatedUtc = DateTime.UtcNow
            },
            new ()
            {
                Id = 5,
                List = list,
                ItemId = itemId1,
                Action = "B",
                UnitPrice = 3,
                Amount = 11,
                CreatedUtc = DateTime.UtcNow.AddHours(5)
            },
            new ()
            {
                Id = 6,
                List = list,
                ItemId = itemId2,
                Action = "B",
                UnitPrice = 5,
                Amount = 5,
                CreatedUtc = DateTime.UtcNow
            }
        };

        // Act
        var snapshotResponse = ItemListMapper.ListSnapshotResponse(snapshot, itemAction, prices);
        Assert.False(snapshotResponse.IsError);
        var snapshotResponseValue = snapshotResponse.Value;
        Assert.Equal(58, snapshotResponseValue.InvestedCapital);
        Assert.Equal(16, snapshotResponseValue.ItemCount);
        Assert.Equal(200, snapshotResponseValue.SalesValue);
        Assert.Equal(154, snapshotResponseValue.Profit);
        Assert.Equal(52, snapshotResponseValue.SteamSellPrice);
        Assert.Equal(84, snapshotResponseValue.Buff163SellPrice);
        _outputHelper.WriteLine(snapshotResponseValue.ToString());
    }
}