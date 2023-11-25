using infrastructure.ExchangeRates;
using TestHelper.TestConfigurationFolder;
using Xunit.Abstractions;

namespace infrastructureTests;

public class ExchangeRateServiceTest(ITestOutputHelper output)
{
    [Fact]
    public async Task GetExchangeRatesTest()
    {
        var configuration = TestConfiguration.GetApiAppSettingsTest();
        var exchangeRatesService = new ExchangeRatesService(new HttpClient(), configuration);
        var exchangeRate = await exchangeRatesService.GetUsdEurExchangeRate();
        if (exchangeRate.IsError)
        {
            Assert.Fail($"Failed to get exchange rate. {exchangeRate.FirstError.Description}");
        }

        output.WriteLine($"{exchangeRate.Value} usd to euro exchange rate found");
        Assert.True(exchangeRate.Value > 0);
    }
}