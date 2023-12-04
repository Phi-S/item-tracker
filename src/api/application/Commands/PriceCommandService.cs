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
    private readonly ItemListSnapshotRepo _itemListSnapshotRepo;

    public PriceCommandService(
        ILogger<PriceCommandService> logger,
        ItemsService itemsService,
        ItemPriceService itemPriceService,
        ExchangeRatesService exchangeRatesService,
        ItemPriceRepo itemPriceRepo,
        ItemListSnapshotRepo itemListSnapshotRepo)
    {
        _logger = logger;
        _itemsService = itemsService;
        _itemPriceService = itemPriceService;
        _exchangeRatesService = exchangeRatesService;
        _itemPriceRepo = itemPriceRepo;
        _itemListSnapshotRepo = itemListSnapshotRepo;
    }

    public async Task<ErrorOr<Success>> RefreshItemPrices()
    {
        _logger.LogInformation("Starting to refresh item prices");
        var allItems = _itemsService.GetAll();
        if (allItems.IsError)
        {
            return allItems.FirstError;
        }

        var prices = await _itemPriceService.GetPrices();
        if (prices.IsError)
        {
            return prices.FirstError;
        }

        var (steamPrices, buff163Prices) = prices.Value;

        var exchangeRate = await _exchangeRatesService.GetUsdEurExchangeRate();
        if (exchangeRate.IsError)
        {
            return exchangeRate.FirstError;
        }

        var priceRefresh = await _itemPriceRepo.CreateNew(
            steamPrices.LastModified,
            buff163Prices.LastModified
        );


        var dbPrices = new ConcurrentBag<ItemPriceDbModel>();
        var formatPriceTasks = new List<Task>();
        foreach (var item in allItems.Value)
        {
            formatPriceTasks.Add(Task.Run(() =>
            {
                var steamPrice = steamPrices.Prices.Where(price => price.itemName.Equals(item.Name))
                    .Select(price => price.price).FirstOrDefault();
                var buffPrice = buff163Prices.Prices.Where(price => price.itemName.Equals(item.Name))
                    .Select(price => price.price).FirstOrDefault();

                decimal? eurSteamPrice = null;
                if (steamPrice is not null)
                {
                    eurSteamPrice = steamPrice.Value * (decimal)exchangeRate.Value;
                }

                decimal? eurBuffPrice = null;
                if (buffPrice is not null)
                {
                    eurBuffPrice = buffPrice.Value * (decimal)exchangeRate.Value;
                }

                var dbPrice = new ItemPriceDbModel
                {
                    ItemId = item.Id,
                    SteamPriceUsd = steamPrice,
                    SteamPriceEur = eurSteamPrice,
                    Buff163PriceUsd = buffPrice,
                    Buff163PriceEur = eurBuffPrice,
                    ItemPriceRefresh = priceRefresh
                };
                dbPrices.Add(dbPrice);
            }));
        }

        await Task.WhenAll(formatPriceTasks);
        await _itemPriceRepo.Add(dbPrices);
        await _itemListSnapshotRepo.CalculateLatestForAll(priceRefresh);
        _logger.LogInformation("Item prices refreshed");
        return Result.Success;
    }
}