using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using shared.Currencies;

namespace infrastructure.Database.Repos;

public class ItemListSnapshotRepo
{
    private readonly XDbContext _dbContext;

    public ItemListSnapshotRepo(XDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CalculateLatestForAll(ItemPriceRefreshDbModel itemPriceRefreshDbModel)
    {
        var lists = _dbContext.Lists.Where(list => list.Deleted == false).ToList();
        foreach (var list in lists)
        {
            await Calculate(list, itemPriceRefreshDbModel);
        }
    }

    public async Task<ItemListSnapshotDbModel> CalculateWithLatestPrices(ItemListDbModel list)
    {
        var latestItemPriceRefresh =
            _dbContext.PricesRefresh.OrderByDescending(refresh => refresh.CreatedUtc).First();
        return await Calculate(list, latestItemPriceRefresh);
    }

    private record CalculateForItemResult(long? TotalSteamPrice, long? TotalBuff163Price);

    private async Task<CalculateForItemResult> CalculateForItem(
        long itemId,
        long itemCount,
        ItemPriceRefreshDbModel itemPriceRefresh,
        string currency)
    {
        if (itemCount == 0)
        {
            return new CalculateForItemResult(null, null);
        }

        var itemPrice = await _dbContext.Prices.FirstOrDefaultAsync(price =>
            price.ItemPriceRefresh == itemPriceRefresh &&
            price.ItemId == itemId
        );

        if (itemPrice is null)
        {
            return new CalculateForItemResult(null, null);
        }

        long? totalSteamPrice;
        long? totalBuff163Price;
        if (currency.Equals(CurrenciesConstants.EURO))
        {
            var eurToUsdExchangeRate = itemPriceRefresh.UsdToEurExchangeRate;
            totalSteamPrice = itemPrice.SteamPriceCentsUsd is null
                ? null
                : (long)Math.Round(itemPrice.SteamPriceCentsUsd.Value * eurToUsdExchangeRate, 0) * itemCount;

            totalBuff163Price = itemPrice.Buff163PriceCentsUsd is null
                ? null
                : (long)Math.Round(itemPrice.Buff163PriceCentsUsd.Value * eurToUsdExchangeRate, 0) * itemCount;
        }
        else if (currency.Equals(CurrenciesConstants.USD))
        {
            totalSteamPrice = itemPrice.SteamPriceCentsUsd * itemCount;
            totalBuff163Price = itemPrice.Buff163PriceCentsUsd * itemCount;
        }
        else
        {
            throw new UnknownCurrencyException(currency);
        }

        return new CalculateForItemResult(totalSteamPrice, totalBuff163Price);
    }

    private async Task<ItemListSnapshotDbModel> Calculate(
        ItemListDbModel list,
        ItemPriceRefreshDbModel itemPriceRefreshDbModel)
    {
        var items = _dbContext.ItemActions
            .Where(item => item.List.Id == list.Id)
            .GroupBy(item => item.ItemId)
            .Select(group => new
            {
                ItemId = group.Key,
                ItemCount =
                    group.Where(itemAction => itemAction.Action.Equals("B")).Sum(itemAction => itemAction.Amount) -
                    group.Where(itemAction => itemAction.Action.Equals("S")).Sum(itemAction => itemAction.Amount)
            })
            .Where(select => select.ItemCount > 0)
            .ToList();

        long? steamValue = null;
        long? buffValue = null;
        foreach (var item in items)
        {
            var forItem = await CalculateForItem(
                item.ItemId,
                item.ItemCount,
                itemPriceRefreshDbModel,
                list.Currency
            );

            if (forItem.TotalSteamPrice is not null)
            {
                steamValue ??= 0;
                steamValue += forItem.TotalSteamPrice.Value;
            }

            if (forItem.TotalBuff163Price is not null)
            {
                buffValue ??= 0;
                buffValue += forItem.TotalBuff163Price.Value;
            }
        }

        var listValue = new ItemListSnapshotDbModel
        {
            List = list,
            SteamValue = steamValue,
            BuffValue = buffValue,
            ItemPriceRefresh = itemPriceRefreshDbModel,
            CreatedUtc = DateTime.UtcNow
        };
        var newItemListValue = await _dbContext.ListSnapshots.AddAsync(listValue);
        return newItemListValue.Entity;
    }
}