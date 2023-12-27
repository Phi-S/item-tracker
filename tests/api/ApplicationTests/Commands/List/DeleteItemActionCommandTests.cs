using application.Commands.List;
using infrastructure.Database;
using infrastructure.Database.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Commands.List;

public class DeleteItemActionCommandTests
{
    private readonly ITestOutputHelper _outputHelper;

    public DeleteItemActionCommandTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task DeleteItemActionCommandTest_OK()
    {
        // Arrange
        var serviceCollection = await ServicesSetup.GetApiApplicationCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        
        const string userId = "test_userid";
        var itemActionId = Random.Shared.NextInt64();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            UserId = userId,
            Name = "test_listname",
            Description = null,
            Url = "test_listurl",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });

        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            Id = itemActionId,
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            UnitPrice = 1,
            Amount = 1,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();
        
        Assert.Single(dbContext.ItemActions);
        Assert.NotNull(await dbContext.ItemActions.FindAsync(itemActionId));
        

        // Act
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new DeleteItemActionCommand(userId, itemActionId);
        var result = await mediator.Send(command);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }

        Assert.Empty(dbContext.ItemActions);
    }
}