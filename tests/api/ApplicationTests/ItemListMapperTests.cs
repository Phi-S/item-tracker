using application.Mapper;
using infrastructure.Database.Models;
using infrastructure.Items;
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
    public void RefreshItemPricesTest_Simple()
    {
        // Arrange
        var item = new ItemModel(1, "1", "item_1", "item_1_hash", "item_1_url", "item_1_image");
        var itemAction = new List<ItemListItemActionDbModel>
        {
            new()
            {
                Id = 0,
                List = null!,
                ItemId = item.Id,
                Action = "B",
                UnitPrice = 1,
                Amount = 1,
                CreatedUtc = default
            }
        };

        // Act
        var listItemResponse = ItemListMapper.MapListItemResponse(item, itemAction);

        // Assert
        Assert.Equal(item.Id, listItemResponse.ItemId);
        Assert.Equal(item.Name, listItemResponse.ItemName);
        Assert.Equal(item.Image, listItemResponse.ItemImage);
        Assert.Equal(1, listItemResponse.CapitalInvested);
        Assert.Equal(1, listItemResponse.AmountInvested);
        Assert.Equal(1, listItemResponse.AverageBuyPrice);
        Assert.Equal(0, listItemResponse.SalesValue);
        Assert.Equal(0, listItemResponse.Profit);
        Assert.Equal(itemAction.Count, listItemResponse.Actions.Count);
    }

    [Fact]
    public void RefreshItemPricesTest_Advanced_1()
    {
        // Arrange
        var item = new ItemModel(1, "1", "item_1", "item_1_hash", "item_1_url", "item_1_image");
        var itemAction = new List<ItemListItemActionDbModel>
        {
            new()
            {
                Id = 0,
                List = null!,
                ItemId = item.Id,
                Action = "B",
                UnitPrice = 1,
                Amount = 1,
                CreatedUtc = default
            },
            new()
            {
                Id = 1,
                List = null!,
                ItemId = item.Id,
                Action = "B",
                UnitPrice = 44,
                Amount = 1,
                CreatedUtc = default
            }
        };

        // Act
        var listItemResponse = ItemListMapper.MapListItemResponse(item, itemAction);

        // Assert
        Assert.Equal(item.Id, listItemResponse.ItemId);
        Assert.Equal(item.Name, listItemResponse.ItemName);
        Assert.Equal(item.Image, listItemResponse.ItemImage);
        Assert.Equal(45, listItemResponse.CapitalInvested);
        Assert.Equal(2, listItemResponse.AmountInvested);
        Assert.Equal(22, listItemResponse.AverageBuyPrice);
        Assert.Equal(0, listItemResponse.SalesValue);
        Assert.Equal(0, listItemResponse.Profit);
        Assert.Equal(itemAction.Count, listItemResponse.Actions.Count);
    }

    [Fact]
    public void RefreshItemPricesTest_Advanced_2()
    {
        // Arrange
        var item = new ItemModel(1, "1", "item_1", "item_1_hash", "item_1_url", "item_1_image");
        var itemAction = new List<ItemListItemActionDbModel>
        {
            new()
            {
                Id = 0,
                List = null!,
                ItemId = item.Id,
                Action = "B",
                UnitPrice = 1,
                Amount = 1,
                CreatedUtc = default
            },
            new()
            {
                Id = 1,
                List = null!,
                ItemId = item.Id,
                Action = "B",
                UnitPrice = 44,
                Amount = 1,
                CreatedUtc = default
            },
            new()
            {
                Id = 1,
                List = null!,
                ItemId = item.Id,
                Action = "S",
                UnitPrice = 100,
                Amount = 2,
                CreatedUtc = default
            },
            new()
            {
                Id = 1,
                List = null!,
                ItemId = item.Id,
                Action = "B",
                UnitPrice = 3,
                Amount = 11,
                CreatedUtc = default
            }
        };

        // Act
        var listItemResponse = ItemListMapper.MapListItemResponse(item, itemAction);

        // Assert
        Assert.Equal(item.Id, listItemResponse.ItemId);
        Assert.Equal(item.Name, listItemResponse.ItemName);
        Assert.Equal(item.Image, listItemResponse.ItemImage);
        Assert.Equal(33, listItemResponse.CapitalInvested);
        Assert.Equal(11, listItemResponse.AmountInvested);
        Assert.Equal(3, listItemResponse.AverageBuyPrice);
        Assert.Equal(200, listItemResponse.SalesValue);
        Assert.Equal(156, listItemResponse.Profit);
        Assert.Equal(itemAction.Count, listItemResponse.Actions.Count);
    }

    private (List<ItemListItemActionDbModel> actions, long expectedAmountInvested, long expectedSalesValue)
        GenerateRandomItemAction(ItemModel itemModel)
    {
        long expectedAmountInvested = 0;
        long expectedSalesValue = 0;

        var buyPrices = new List<long>();
        var random = new Random();
        var itemActions = new List<ItemListItemActionDbModel>();
        for (var i = 0; i < random.Next(10, 1000); i++)
        {
            var sellBuy = random.Next() % 2 == 0 ? "B" : "S";
            var amount = random.Next(10000);
            if (sellBuy.Equals("S"))
            {
                var buyCount = itemActions.Where(action => action.Action.Equals("B")).Select(action => action.Amount)
                    .Sum();
                var sellCount = itemActions.Where(action => action.Action.Equals("S")).Select(action => action.Amount)
                    .Sum();
                if (buyCount - sellCount - amount < 0)
                {
                    continue;
                }
            }

            var itemAction = new ItemListItemActionDbModel
            {
                Id = 0,
                List = null!,
                ItemId = itemModel.Id,
                Action = sellBuy,
                UnitPrice = random.Next(1000000),
                Amount = amount,
                CreatedUtc = DateTime.UtcNow.AddSeconds(5)
            };
            itemActions.Add(itemAction);
            if (itemAction.Action.Equals("B"))
            {
                expectedAmountInvested += itemAction.Amount;
            }
            else if (itemAction.Action.Equals("S"))
            {
                expectedSalesValue += itemAction.UnitPrice * itemAction.Amount;
                expectedAmountInvested -= itemAction.Amount;
            }
        }

        return (itemActions, expectedAmountInvested, expectedSalesValue);
    }

    [Fact]
    public void RefreshItemPricesTest_Random()
    {
        // Arrange
        var item = new ItemModel(1, "1", "item_1", "item_1_hash", "item_1_url", "item_1_image");
        var (generatedItemAction, expectedAmountInvested, expectedSalesValue) = GenerateRandomItemAction(item);

        // Act
        var listItemResponse = ItemListMapper.MapListItemResponse(item, generatedItemAction);

        // Assert
        Assert.True(generatedItemAction.Count > 0);
        Assert.True(listItemResponse.Actions.Count > 0);
        Assert.Equal(item.Id, listItemResponse.ItemId);
        Assert.Equal(item.Name, listItemResponse.ItemName);
        Assert.Equal(item.Image, listItemResponse.ItemImage);
        Assert.Equal(generatedItemAction.Count, listItemResponse.Actions.Count);
        Assert.Equal(expectedAmountInvested, listItemResponse.AmountInvested);
        Assert.Equal(expectedSalesValue, listItemResponse.SalesValue);
        _outputHelper.WriteLine(listItemResponse.ToString());
    }
}