using System.Diagnostics;
using infrastructure.ItemPriceFolder;
using Xunit.Abstractions;

namespace infrastructureTests;

public class SteamPriceServiceTest(ITestOutputHelper output)
{
    [Fact]
    public async Task GetPriceTest()
    {
        var priceService = new ItemPriceService(new HttpClient());
        var sw = Stopwatch.StartNew();
        var prices = await priceService.GetPrices();
        if (prices.IsError)
        {
            output.WriteLine(prices.FirstError.ToString());
            Assert.Fail("Failed to get prices");
        }
        sw.Stop();
        output.WriteLine($"{prices.Value.Count} prices found");
        output.WriteLine($"priceService.GetPrices duration: {sw.ElapsedMilliseconds}");
        Assert.True(prices.Value.Count != 0);
    }
}