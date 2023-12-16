using System.Collections.Concurrent;
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
    private readonly UnitOfWork _unitOfWork;

    public PriceCommandService(
        ILogger<PriceCommandService> logger,
        ItemsService itemsService,
        ItemPriceService itemPriceService,
        ExchangeRatesService exchangeRatesService,
        UnitOfWork unitOfWork)
    {
        _logger = logger;
        _itemsService = itemsService;
        _itemPriceService = itemPriceService;
        _exchangeRatesService = exchangeRatesService;
        _unitOfWork = unitOfWork;
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

        var usdEurExchangeRate = await _exchangeRatesService.GetUsdEurExchangeRate();
        if (usdEurExchangeRate.IsError)
        {
            return usdEurExchangeRate.FirstError;
        }

        var priceRefresh = await _unitOfWork.ItemPriceRepo.CreateNew(
            Math.Round(usdEurExchangeRate.Value, 2, MidpointRounding.ToZero),
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
                var buff163Price = buff163Prices.Prices.Where(price => price.itemName.Equals(item.Name))
                    .Select(price => price.price).FirstOrDefault();

                var dbPrice = new ItemPriceDbModel
                {
                    ItemId = item.Id,
                    SteamPriceCentsUsd = steamPrice is null ? null : (int)(steamPrice.Value * 100),
                    Buff163PriceCentsUsd = buff163Price is null ? null : (int)(buff163Price.Value * 100),
                    ItemPriceRefresh = priceRefresh
                };
                dbPrices.Add(dbPrice);
            }));
        }

        await Task.WhenAll(formatPriceTasks);
        await _unitOfWork.ItemPriceRepo.Add(dbPrices);
        await _unitOfWork.ItemListRepo.NewSnapshotForEveryList(priceRefresh);
        await _unitOfWork.Save();
        _logger.LogInformation("Item prices refreshed");
        return Result.Success;
    }
}