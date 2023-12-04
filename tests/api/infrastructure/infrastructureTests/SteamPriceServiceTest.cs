using System.Diagnostics;
using infrastructure.ItemPriceFolder;
using infrastructure.Items;
using Xunit.Abstractions;

namespace infrastructureTests;

public class SteamPriceServiceTest
{
    private readonly ITestOutputHelper _output;

    public SteamPriceServiceTest(ITestOutputHelper outputHelper)
    {
        _output = outputHelper;
    }

    [Fact]
    public async Task GetPriceTest()
    {
        var priceService = new ItemPriceService(new HttpClient());
        var sw = Stopwatch.StartNew();
        var pricesResult = await priceService.GetPrices();
        if (pricesResult.IsError)
        {
            _output.WriteLine(pricesResult.FirstError.ToString());
            Assert.Fail("Failed to get prices");
        }

        sw.Stop();
        var pricesModel = pricesResult.Value;
        _output.WriteLine($"{pricesModel.steamPrices.Prices.Count} steam prices found");
        _output.WriteLine($"{pricesModel.buff163Prices.Prices.Count} buff prices found");
        _output.WriteLine($"priceService.GetPrices duration: {sw.ElapsedMilliseconds}");
        Assert.True(pricesModel.steamPrices.Prices.Count != 0);
        Assert.True(pricesModel.buff163Prices.Prices.Count != 0);
    }

    [Fact]
    public async Task AllItemsGotPricesTest()
    {
        var priceService = new ItemPriceService(new HttpClient());
        var pricesResult = await priceService.GetPrices();
        if (pricesResult.IsError)
        {
            Assert.Fail(pricesResult.FirstError.Description);
        }

        var (steamPrices, buff163Prices) = pricesResult.Value;

        var itemsService = new ItemsService();
        var allItems = itemsService.GetAll();
        if (allItems.IsError)
        {
            Assert.Fail(allItems.FirstError.Description);
        }

        var itemsNotFoundInSteamPrices = 0;
        var itemsNotFoundInBuff163Prices = 0;
        var itemsNotFoundInBuff163AndSteam = 0;

        foreach (var item in allItems.Value)
        {
            var steamPriceForItem = steamPrices.Prices.Any(price => price.itemName.Equals(item.Name));
            var buffPriceForItem = buff163Prices.Prices.Any(price => price.itemName.Equals(item.Name));

            if (steamPriceForItem == false && buffPriceForItem == false)
            {
                _output.WriteLine($"Both prices Missing item \"{item.Id} | {item.Name}\"");
                itemsNotFoundInBuff163AndSteam++;
            }
            else if (steamPriceForItem == false)
            {
                _output.WriteLine($"Steam price missing item \"{item.Id} | {item.Name}\"");
                itemsNotFoundInSteamPrices++;
            }
            else if (buffPriceForItem == false)
            {
                _output.WriteLine($"Buff163 price missing item \"{item.Id} | {item.Name}\"");
                itemsNotFoundInBuff163Prices++;
            }
        }

        Assert.True(allItems.Value.Count > 0, "No items found");
        _output.WriteLine($"Item count: {allItems.Value.Count}");
        _output.WriteLine($"Steam prices count: {steamPrices.Prices.Count}");
        _output.WriteLine($"Buff163 prices count: {buff163Prices.Prices.Count}");

        Assert.True(
            itemsNotFoundInBuff163AndSteam == 0 &&
            itemsNotFoundInSteamPrices == 0 &&
            itemsNotFoundInBuff163Prices == 0,
            $"Prices missing for items:\n" +
            $"Missing from both steam and buff163 prices: {itemsNotFoundInBuff163AndSteam}\n" +
            $"Missing from steam prices: {itemsNotFoundInSteamPrices}\n" +
            $"Missing from buff163 prices: {itemsNotFoundInBuff163Prices}"
        );
    }
}