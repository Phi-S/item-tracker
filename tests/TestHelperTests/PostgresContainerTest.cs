using TestHelper.DockerContainerFolder;
using Xunit.Abstractions;

namespace TestHelperTests;

public class PostgresContainerTest
{
    private readonly ITestOutputHelper _outputHelper;

    public PostgresContainerTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task StartPostgresTest()
    {
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
    }
}