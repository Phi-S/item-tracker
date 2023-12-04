using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Database.Repos;

public class ItemListSnapshotRepo(XDbContext dbContext)
{
    public async Task CalculateLatestForAll(ItemPriceRefreshDbModel itemPriceRefreshDbModel)
    {
        var lists = dbContext.ItemLists.Where(list => list.Deleted == false).ToList();
        foreach (var list in lists)
        {
            await Calculate(list, itemPriceRefreshDbModel);
        }
    }

    public async Task<ItemListSnapshotDbModel> CalculateWithLatestPrices(ItemListDbModel list)
    {
        var latestItemPriceRefresh =
            dbContext.ItemPriceRefresh.OrderByDescending(refresh => refresh.CreatedUtc).First();
        return await Calculate(list, latestItemPriceRefresh);
    }

    private async Task<ItemListSnapshotDbModel> Calculate(
        ItemListDbModel list,
        ItemPriceRefreshDbModel itemPriceRefreshDbModel)
    {
        var itemsInList = dbContext.ItemListItemAction
            .Where(item => item.List.Id == list.Id)
            .GroupBy(item => item.ItemId)
            .ToList();

        // TODO: replace by single decimal to avoid multiple enumerations later with sum
        var steamValues = new List<decimal>();
        var buffValues = new List<decimal>();
        decimal investedCapital = 0;
        var listItemCount = 0;
        foreach (var itemActions in itemsInList)
        {
            var itemCount = 0;
            var buyPrices = new List<decimal>();
            foreach (var itemAction in itemActions.OrderBy(action => action.CreatedUtc))
            {
                if (itemAction.Action.Equals("B"))
                {
                    if (itemCount == 0)
                    {
                        buyPrices.Clear();
                    }

                    buyPrices.AddRange(Enumerable.Repeat(itemAction.PricePerOne, itemAction.Amount));
                    itemCount += itemAction.Amount;
                }
                else if (itemAction.Action.Equals("S"))
                {
                    itemCount -= itemAction.Amount;
                }
            }

            var averageBuyPrice = buyPrices.Average();
            investedCapital += averageBuyPrice * itemCount;
            listItemCount += itemCount;

            var itemPrice = await dbContext.ItemPrices.FirstAsync(price =>
                price.ItemPriceRefresh.Id == itemPriceRefreshDbModel.Id &&
                price.ItemId == itemActions.Key
            );

            if (list.Currency == "EUR")
            {
                if (itemPrice.SteamPriceEur is not null)
                {
                    steamValues.Add(itemPrice.SteamPriceEur.Value * itemCount);
                }

                if (itemPrice.Buff163PriceEur is not null)
                {
                    buffValues.Add(itemPrice.Buff163PriceEur.Value * itemCount);
                }
            }
            else if (list.Currency == "USD")
            {
                if (itemPrice.SteamPriceUsd is not null)
                {
                    steamValues.Add(itemPrice.SteamPriceUsd.Value * itemCount);
                }

                if (itemPrice.Buff163PriceUsd is not null)
                {
                    buffValues.Add(itemPrice.Buff163PriceUsd.Value * itemCount);
                }
            }
        }

        var listValue = new ItemListSnapshotDbModel
        {
            List = list,
            InvestedCapital = investedCapital,
            ItemCount = listItemCount,
            SteamValue = steamValues.Count != 0
                ? steamValues.Sum()
                : null,
            BuffValue = buffValues.Count != 0
                ? buffValues.Sum()
                : null,
            ItemPriceRefresh = itemPriceRefreshDbModel,
            CreatedUtc = DateTime.UtcNow
        };
        var newItemListValue = await dbContext.ItemListValues.AddAsync(listValue);
        await dbContext.SaveChangesAsync();
        return newItemListValue.Entity;
    }
}