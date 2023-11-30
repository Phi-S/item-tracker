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
    private readonly ItemListItemRepo _itemListItemRepo;
    private readonly ItemListRepo _itemListRepo;
    private readonly ItemListValueRepo _itemListValueRepo;

    public PriceCommandService(
        ILogger<PriceCommandService> logger,
        ItemsService itemsService,
        ItemPriceService itemPriceService,
        ExchangeRatesService exchangeRatesService,
        ItemPriceRepo itemPriceRepo,
        ItemListItemRepo itemListItemRepo,
        ItemListRepo itemListRepo,
        ItemListValueRepo itemListValueRepo)
    {
        _logger = logger;
        _itemsService = itemsService;
        _itemPriceService = itemPriceService;
        _exchangeRatesService = exchangeRatesService;
        _itemPriceRepo = itemPriceRepo;
        _itemListItemRepo = itemListItemRepo;
        _itemListRepo = itemListRepo;
        _itemListValueRepo = itemListValueRepo;
    }

    public async Task<ErrorOr<Success>> RefreshItemPrices()
    {
        var createdAt = DateTime.UtcNow;
        var prices = await _itemPriceService.GetPrices();
        if (prices.IsError)
        {
            return prices.FirstError;
        }

        var exchangeRate = await _exchangeRatesService.GetUsdEurExchangeRate();
        if (exchangeRate.IsError)
        {
            return exchangeRate.FirstError;
        }

        var dbPrices = new List<ItemPriceDbModel>();
        var itemPricesNotFountByItemsService = new List<PriceModel>();
        foreach (var price in prices.Value)
        {
            var item = _itemsService.GetByName(price.Name);
            if (item.IsError)
            {
                itemPricesNotFountByItemsService.Add(price);
                continue;
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
                CreatedUtc = createdAt
            };
            dbPrices.Add(dbPrice);
        }

        _logger.LogDebug(
            "Following prices are received but no item could be assigned to it\n {ItemPricesNotFountByItemsService}",
            string.Join("\n", itemPricesNotFountByItemsService));
        await _itemPriceRepo.Add(dbPrices);

        var refreshListValues = await RefreshListValues(createdAt, dbPrices);
        if (refreshListValues.IsError)
        {
            return refreshListValues.FirstError;
        }

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> RefreshListValues(DateTime dateOfPrices, List<ItemPriceDbModel> prices)
    {
        var allLists = _itemListRepo.All();
        foreach (var list in allLists)
        {
            var itemsInList = await _itemListItemRepo.GetItemsForList(list);
            var totalItemsValue = GetTotalItemsValue(itemsInList, prices, list.Currency);
            if (totalItemsValue.IsError)
            {
                return totalItemsValue.FirstError;
            }

            var listValue = new ItemListValueDbModel()
            {
                ItemListDbModel = list,
                SteamValue = totalItemsValue.Value.steamValue,
                BuffValue = totalItemsValue.Value.buffValue,
                CreatedUtc = dateOfPrices
            };
            await _itemListValueRepo.Add(listValue);
        }

        return Result.Success;
    }

    public static ErrorOr<(decimal? steamValue, decimal? buffValue)> GetTotalItemsValue(
        IEnumerable<ItemListItemActionDbModel> items,
        IReadOnlyCollection<ItemPriceDbModel> prices,
        string currency)
    {
        var groupByItemId = items.GroupBy(item => item.ItemId);

        var steamValue = new List<decimal>();
        var buffValue = new List<decimal>();
        foreach (var itemActionsGroup in groupByItemId)
        {
            var itemPrice = prices.FirstOrDefault(price => price.ItemId == itemActionsGroup.Key);
            if (itemPrice is null)
            {
                return Error.Failure($"Price for the item with the id \"{itemActionsGroup.Key}\" not found");
            }

            var actions = itemActionsGroup.ToList();
            long totalItemCount = 0;
            foreach (var itemAction in actions)
            {
                if (itemAction.Action.Equals("B"))
                {
                    totalItemCount += itemAction.Amount;
                }
                else if (itemAction.Action.Equals("S"))
                {
                    totalItemCount -= itemAction.Amount;
                }
            }

            switch (currency)
            {
                case "EUR":
                {
                    if (itemPrice.SteamPriceEur is not null)
                    {
                        steamValue.Add(itemPrice.SteamPriceEur.Value * totalItemCount);
                    }

                    if (itemPrice.BuffPriceEur is not null)
                    {
                        buffValue.Add(itemPrice.BuffPriceEur.Value * totalItemCount);
                    }

                    break;
                }
                case "USD":
                {
                    if (itemPrice.SteamPriceUsd is not null)
                    {
                        steamValue.Add(itemPrice.SteamPriceUsd.Value * totalItemCount);
                    }

                    if (itemPrice.BuffPriceUsd is not null)
                    {
                        buffValue.Add(itemPrice.BuffPriceUsd.Value * totalItemCount);
                    }

                    break;
                }
            }
        }

        decimal? steamValueResult = steamValue.Count != 0 ? steamValue.Sum() : null;
        decimal? buffValueResult = buffValue.Count != 0 ? buffValue.Sum() : null;
        return (steamValueResult, buffValueResult);
    }
}