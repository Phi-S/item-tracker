using application.Commands.List;
using infrastructure.Database;
using infrastructure.Database.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.RandomHelperFolder;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Commands.List;

public class UpdateListPublicCommandTests
{
    private readonly ITestOutputHelper _outputHelper;

    public UpdateListPublicCommandTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task UpdateListPublicCommandTest_OK()
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
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new UpdateListPublicCommand(userId, listUrl, true);
        var result = await mediator.Send(command);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }

        Assert.Single(dbContext.Lists);
        var listInDb = dbContext.Lists.First();
        Assert.True(listInDb.Public);
    }
}