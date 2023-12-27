using application.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.RandomHelperFolder;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Queries;

public class GetAllListsForUserQueryTests
{
    private readonly ITestOutputHelper _outputHelper;

    public GetAllListsForUserQueryTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task GetAllListsForUserQueryTest_Ok()
    {
        // TODO:
        // Arrange
        var serviceCollection = await ServicesSetup.GetApiApplicationCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();

        var userId = RandomHelper.RandomString();

        // Act
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new GetAllListsForUserQuery(userId);
        var result = await mediator.Send(command);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }
    }
}