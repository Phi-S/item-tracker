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
        Assert.Equal(33, listItemResponse.CapitalInvested);
        Assert.Equal(11, listItemResponse.AmountInvested);
        Assert.Equal(3, listItemResponse.AverageBuyPrice);
        Assert.Equal(200, listItemResponse.SalesValue);
        Assert.Equal(156, listItemResponse.Profit);
        Assert.Equal(2, listItemResponse.SteamSellPrice);
        Assert.Equal(4, listItemResponse.Buff163SellPrice);
        Assert.Equal(itemAction.Count, listItemResponse.Actions.Count);
        _outputHelper.WriteLine(listItemResponse.ToString());
    }
}