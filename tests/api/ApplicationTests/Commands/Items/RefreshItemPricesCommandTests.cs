using System.Diagnostics;
using application.Commands.Items;
using infrastructure.Database;
using infrastructure.Database.Models;
using infrastructure.Items;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Commands.Items;

public class RefreshItemPricesCommandTests
{
    private readonly ITestOutputHelper _outputHelper;

    public RefreshItemPricesCommandTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task RefreshItemPricesCommandTest()
    {
        // Arrange
        var serviceCollection = await ServicesSetup.GetApiApplicationCollection(_outputHelper);
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
        await dbContext.SaveChangesAsync();

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
        await dbContext.SaveChangesAsync();

        // Act
        var mediator = provider.GetRequiredService<IMediator>();
        var refreshItemPricesCommand = new RefreshItemPricesCommand();
        var sw = Stopwatch.StartNew();
        var result = await mediator.Send(refreshItemPricesCommand);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }

        sw.Stop();
        var itemPricesCount = dbContext.Prices.Count();
        Assert.True(itemPricesCount > 0, $"{itemPricesCount} item prices added to database");
        _outputHelper.WriteLine($"{itemPricesCount} item prices added to database");

        var itemsService = provider.GetRequiredService<ItemsService>();
        var allItems = itemsService.GetAll();
        if (allItems.IsError)
        {
            Assert.Fail(allItems.FirstError.Description);
        }

        Assert.True(allItems.Value.Count == itemPricesCount, "Not all items got prices");
        _outputHelper.WriteLine($"RefreshItemPrices duration: {sw.ElapsedMilliseconds}");
        Assert.True(true);
    }
}