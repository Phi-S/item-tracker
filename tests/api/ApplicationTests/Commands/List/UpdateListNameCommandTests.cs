using application.Commands.List;
using infrastructure.Database;
using infrastructure.Database.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.RandomHelperFolder;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Commands.List;

public class UpdateListNameCommandTests
{
    private readonly ITestOutputHelper _outputHelper;

    public UpdateListNameCommandTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task UpdateListNameCommandTest_OK()
    {
        // Arrange
        var serviceCollection = await ServicesSetup.GetApiApplicationCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();

        var dbContext = provider.GetRequiredService<XDbContext>();
        var userId = RandomHelper.RandomString();
        var listUrl = RandomHelper.RandomString();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            UserId = userId,
            Name = RandomHelper.RandomString(),
            Url = listUrl,
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();
        Assert.True(list.Entity.Description is null);

        // Act
        var newName = RandomHelper.RandomString();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new UpdateListNameCommand(userId, listUrl, newName);
        var result = await mediator.Send(command);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }

        Assert.Single(dbContext.Lists);
        var listInDb = dbContext.Lists.First();
        Assert.Equal(newName, listInDb.Name);
    }
}