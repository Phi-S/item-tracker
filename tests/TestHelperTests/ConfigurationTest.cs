using Microsoft.Extensions.Configuration;
using TestHelper.TestConfigurationFolder;
using Xunit.Abstractions;

namespace TestHelperTests;

public class ConfigurationTest
{
    private readonly ITestOutputHelper _outputHelper;

    public ConfigurationTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void GetIConfigurationTest()
    {
        var configuration = TestConfiguration.GetApiAppSettingsTest();
        _outputHelper.WriteLine(string.Join("\n", configuration.AsEnumerable()));
    }

    [Fact]
    public void GetExchangeRateEnvironmentVariable()
    {
        var testExchangeRateApiKey = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable("ExchangeRatesApiKey", testExchangeRateApiKey);
        var configuration = TestConfiguration.GetApiAppSettingsTest();
        var exchangeRateFromIConfiguration = configuration.GetValue<string>("ExchangeRatesApiKey");
        Assert.Equal(testExchangeRateApiKey, exchangeRateFromIConfiguration);
    }
}