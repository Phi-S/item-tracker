using System.Collections.Concurrent;
using System.Diagnostics;
using ErrorOr;
using infrastructure.Database.Models;
using infrastructure.Database.Repos;
using infrastructure.ExchangeRates;
using infrastructure.ItemPriceFolder;
using infrastructure.Items;
using Microsoft.Extensions.Logging;

namespace application.Commands;

public class PriceCommandService
{
    private readonly ILogger<PriceCommandService> _logger;
    private readonly ItemsService _itemsService;
    private readonly ItemPriceService _itemPriceService;
    private readonly ExchangeRatesService _exchangeRatesService;
    private readonly ItemPriceRepo _itemPriceRepo;
    private readonly ItemListValueRepo _itemListValueRepo;

    public PriceCommandService(
        ILogger<PriceCommandService> logger,
        ItemsService itemsService,
        ItemPriceService itemPriceService,
        ExchangeRatesService exchangeRatesService,
        ItemPriceRepo itemPriceRepo,
        ItemListValueRepo itemListValueRepo)
    {
        _logger = logger;
        _itemsService = itemsService;
        _itemPriceService = itemPriceService;
        _exchangeRatesService = exchangeRatesService;
        _itemPriceRepo = itemPriceRepo;
        _itemListValueRepo = itemListValueRepo;
    }

    public async Task<ErrorOr<Success>> RefreshItemPrices()
    {
        _logger.LogInformation("Starting to refresh item prices");
        var sw = Stopwatch.StartNew();
        var prices = await _itemPriceService.GetPrices();
        if (prices.IsError)
        {
            return prices.FirstError;
        }

        _logger.LogInformation("_itemPriceService.GetPrices duration: {ItemPriceServiceGetPricesDuration}",
            sw.ElapsedMilliseconds);
        sw.Restart();
        var exchangeRate = await _exchangeRatesService.GetUsdEurExchangeRate();
        if (exchangeRate.IsError)
        {
            return exchangeRate.FirstError;
        }

        _logger.LogInformation(
            "_exchangeRatesService.GetUsdEurExchangeRate duration: {ExchangeRatesServiceGetUsdEurExchangeRateDuration}",
            sw.ElapsedMilliseconds);
        sw.Restart();

        var priceRefresh = await _itemPriceRepo.CreateNew();
        var dbPrices = new ConcurrentBag<ItemPriceDbModel>();
        var formatPriceTasks = new List<Task>();
        //var itemPricesNotFountByItemsService = new List<PriceModel>();
        foreach (var price in prices.Value)
        {
            formatPriceTasks.Add(Task.Run(() =>
            {
                var item = _itemsService.GetByName(price.Name);
                if (item.IsError)
                {
                    //itemPricesNotFountByItemsService.Add(price);
                    return;
                }

                decimal? eurSteamPrice = null;
                if (price.SteamPrice is not null)
                {
                    eurSteamPrice = price.SteamPrice.Value * (decimal)exchangeRate.Value;
                }

                decimal? eurBuffPrice = null;
                if (price.BuffPrice is not null)
                {
                    eurBuffPrice = price.BuffPrice.Value * (decimal)exchangeRate.Value;
                }

                var dbPrice = new ItemPriceDbModel
                {
                    ItemId = item.Value.Id,
                    SteamPriceUsd = price.SteamPrice,
                    SteamPriceEur = eurSteamPrice,
                    BuffPriceUsd = price.BuffPrice,
                    BuffPriceEur = eurBuffPrice,
                    ItemPriceRefresh = priceRefresh
                };
                dbPrices.Add(dbPrice);
            }));
        }

        await Task.WhenAll(formatPriceTasks);

        _logger.LogInformation(
            "Formatting prices duration: {ExchangeRatesServiceGetUsdEurExchangeRateDuration}",
            sw.ElapsedMilliseconds);
        sw.Restart();

        //_logger.LogDebug(
        //    "Following prices are received but no item could be assigned to it\n {ItemPricesNotFountByItemsService}",
        //    string.Join("\n", itemPricesNotFountByItemsService));

        await _itemPriceRepo.Add(dbPrices);
        _logger.LogInformation("_itemPriceRepo.Add duration: {ItemPriceRepoAddDuration}", sw.ElapsedMilliseconds);
        sw.Restart();
        await _itemListValueRepo.CalculateLatestForAll(priceRefresh);
        _logger.LogInformation(
            "_itemListValueRepo.CalculateLatestForAll duration: {ItemListValueRepoCalculateLatestForAllDuration}",
            sw.ElapsedMilliseconds);
        _logger.LogInformation("Item prices refreshed");
        return Result.Success;
    }
}