using application.Commands.List;
using infrastructure.Database;
using infrastructure.Database.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Commands.List;

public class AddItemActionSellCommandTests
{
    private readonly ITestOutputHelper _outputHelper;

    public AddItemActionSellCommandTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task AddItemActionSellCommandTest()
    {
        // Arrange
        var serviceCollection = await ServicesSetup.GetApiApplicationCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();

        var dbContext = provider.GetRequiredService<XDbContext>();
        const string userId = "test_userid";
        const string listUrl = "test_listurl";
        var itemId = 1;
        var unitPrice = 2;
        var amount = 3;
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            UserId = userId,
            Name = "test_list",
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
            ItemId = itemId,
            Action = "B",
            UnitPrice = 1,
            Amount = amount,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        Assert.Single(dbContext.ItemActions.Where(action => action.List.Id == list.Entity.Id));

        // Act
        var mediator = provider.GetRequiredService<IMediator>();
        var addItemActionSellHandler = new AddItemActionSellCommand(userId, listUrl, itemId, unitPrice, amount);
        var result = await mediator.Send(addItemActionSellHandler);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }

        Assert.Equal(2, dbContext.ItemActions.Count(action => action.List.Id == list.Entity.Id));
        var itemActionInDb = await dbContext.ItemActions
            .Include(itemListItemActionDbModel => itemListItemActionDbModel.List)
            .FirstAsync(action => action.Action.Equals("S"));
        Assert.Equal(list.Entity.Id, itemActionInDb.List.Id);
        Assert.Equal(itemId, itemActionInDb.ItemId);
        Assert.Equal(unitPrice, itemActionInDb.UnitPrice);
        Assert.Equal(amount, itemActionInDb.Amount);
        Assert.Equal("S", itemActionInDb.Action);
    }
}