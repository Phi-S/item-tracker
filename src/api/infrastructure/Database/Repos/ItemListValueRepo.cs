using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Database.Repos;

public class ItemListValueRepo(XDbContext dbContext)
{
    public async Task CalculateLatestForAll(ItemPriceRefreshDbModel? itemPriceRefreshDbModel)
    {
        var lists = dbContext.ItemLists.Where(list => list.Deleted == false).ToList();
        foreach (var list in lists)
        {
            await CalculateLatest(list, itemPriceRefreshDbModel);
        }
    }

    public async Task<ItemListValueDbModel> CalculateLatest(ItemListDbModel list,
        ItemPriceRefreshDbModel? itemPriceRefreshDbModel = null)
    {
        var latestItemPriceRefresh = itemPriceRefreshDbModel;
        if (latestItemPriceRefresh is null)
        {
            if (dbContext.ItemPriceRefresh.Any())
            {
                latestItemPriceRefresh = await dbContext.ItemPriceRefresh.FirstOrDefaultAsync(refresh =>
                    refresh.Id == dbContext.ItemPriceRefresh.Max(price => price.Id));
            }
        }

        var itemsInList = dbContext.ItemListItemAction.Where(item => item.List.Id == list.Id)
            .GroupBy(item => item.ItemId).ToList();

        var steamValues = new List<decimal>();
        var buffValues = new List<decimal>();
        decimal totalCapitalInvested = 0;
        foreach (var itemActions in itemsInList)
        {
            long totalItemCount = 0;
            foreach (var itemAction in itemActions)
            {
                if (itemAction.Action.Equals("B"))
                {
                    totalItemCount += itemAction.Amount;
                    totalCapitalInvested += itemAction.Amount * itemAction.PricePerOne;
                }
                else if (itemAction.Action.Equals("S"))
                {
                    totalItemCount -= itemAction.Amount;
                    totalCapitalInvested -= itemAction.Amount * itemAction.PricePerOne;
                }
            }

            if (latestItemPriceRefresh is null)
            {
                continue;
            }

            var itemPrice = await dbContext.ItemPrices.FirstOrDefaultAsync(price =>
                price.ItemPriceRefresh.Id == latestItemPriceRefresh.Id &&
                price.ItemId == itemActions.Key);
            if (itemPrice is null)
            {
                continue;
            }

            if (list.Currency == "EUR")
            {
                if (itemPrice.SteamPriceEur is not null)
                {
                    steamValues.Add(itemPrice.SteamPriceEur.Value * totalItemCount);
                }

                if (itemPrice.BuffPriceEur is not null)
                {
                    buffValues.Add(itemPrice.BuffPriceEur.Value * totalItemCount);
                }
            }
            else if (list.Currency == "USD")
            {
                if (itemPrice.SteamPriceUsd is not null)
                {
                    steamValues.Add(itemPrice.SteamPriceUsd.Value * totalItemCount);
                }

                if (itemPrice.BuffPriceUsd is not null)
                {
                    buffValues.Add(itemPrice.BuffPriceUsd.Value * totalItemCount);
                }
            }
        }

        var listValue = new ItemListValueDbModel
        {
            List = list,
            SteamValue = steamValues.Count != 0 ? steamValues.Sum() : null,
            BuffValue = buffValues.Count != 0 ? buffValues.Sum() : null,
            InvestedCapital = totalCapitalInvested,
            ItemPriceRefresh = latestItemPriceRefresh,
            CreatedUtc = DateTime.UtcNow
        };
        var newItemListValue = await dbContext.ItemListValues.AddAsync(listValue);
        await dbContext.SaveChangesAsync();
        return newItemListValue.Entity;
    }
}