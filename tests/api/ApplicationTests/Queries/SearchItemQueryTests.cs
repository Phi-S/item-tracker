using application.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests.Queries;

public class SearchItemQueryTests
{
    private readonly ITestOutputHelper _outputHelper;

    public SearchItemQueryTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task SearchItemQueryTest_Ok()
    {
        // Arrange
        var serviceCollection = await ServicesSetup.GetApiApplicationCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();

        // Act
        const string searchQuery = "redline";
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new SearchItemQuery(searchQuery);
        var result = await mediator.Send(command);

        // Assert
        if (result.IsError)
        {
            Assert.Fail(result.FirstError.Description);
        }
        Assert.True(true);
    }
}