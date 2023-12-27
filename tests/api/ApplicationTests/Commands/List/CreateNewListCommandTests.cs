using application.Commands.List;
using infrastructure.Database;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Commands.List;

public class CreateNewListCommandTests
{
    private readonly ITestOutputHelper _outputHelper;

    public CreateNewListCommandTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task CreateNewListCommandTest_OK()
    {
        // Arrange
        var serviceCollection = await ServicesSetup.GetApiApplicationCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();

        var dbContext = provider.GetRequiredService<XDbContext>();
        Assert.Empty(dbContext.Lists);
        
        // Act
        const string userId = "test_userid";
        const string listName = "test_listname";
        const string listDescription = "test_listdescription";
        const string currency = "EUR";
        const bool @public = false;
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new CreateNewListCommand(userId, listName, listDescription, currency, @public);
        var result = await mediator.Send(command);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }

        Assert.False(string.IsNullOrWhiteSpace(result.Value));
        Assert.Single(dbContext.Lists);
        var listInDb = dbContext.Lists.First();
        Assert.Equal(userId, listInDb.UserId);
        Assert.Equal(listName, listInDb.Name);
        Assert.Equal(listDescription, listInDb.Description);
        Assert.Equal(currency, listInDb.Currency);
        Assert.Equal(@public, listInDb.Public);
    }
}