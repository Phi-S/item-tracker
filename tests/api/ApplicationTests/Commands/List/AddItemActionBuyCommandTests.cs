using application.Commands.List;
using infrastructure.Database;
using infrastructure.Database.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Commands.List;

public class AddItemActionBuyCommandTests
{
    private readonly ITestOutputHelper _outputHelper;

    public AddItemActionBuyCommandTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task AddItemActionBuyCommandTest()
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
        await dbContext.SaveChangesAsync();

        Assert.Empty(dbContext.ItemActions.Where(action => action.List.Id == list.Entity.Id));
        
        // Act
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new AddItemActionBuyCommand(userId, listUrl, itemId, unitPrice, amount);
        var result = await mediator.Send(command);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }
        
        Assert.Single(dbContext.ItemActions.Where(action => action.List.Id == list.Entity.Id));
        var itemActionInDb = dbContext.ItemActions.Include(itemListItemActionDbModel => itemListItemActionDbModel.List).First();
        Assert.Equal(list.Entity.Id, itemActionInDb.List.Id);
        Assert.Equal(itemId, itemActionInDb.ItemId);
        Assert.Equal(unitPrice, itemActionInDb.UnitPrice);
        Assert.Equal(amount, itemActionInDb.Amount);
        Assert.Equal("B", itemActionInDb.Action);
    }
}