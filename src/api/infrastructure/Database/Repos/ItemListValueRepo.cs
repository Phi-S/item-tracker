using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Database.Repos;

public class ItemListValueRepo(XDbContext dbContext)
{
    public async Task<List<ItemListValueDbModel>> GetAll(ItemListDbModel listDbModel)
    {
        return await dbContext.ItemListValues.Where(value => value.List.Id == listDbModel.Id).ToListAsync();
    }

    public async Task Add(ItemListValueDbModel itemListValueDbModel)
    {
        await dbContext.ItemListValues.AddAsync(itemListValueDbModel);
        await dbContext.SaveChangesAsync();
    }

    public async Task CalculateLatestForAll()
    {
        var lists = dbContext.ItemLists.Where(list => list.Deleted == false).ToList();
        foreach (var list in lists)
        {
            await CalculateLatest(list);
        }
    }

    public async Task<ItemListValueDbModel> CalculateLatest(ItemListDbModel list)
    {
        var latestItemPrices = dbContext.ItemPrices.Where(price =>
                price.ItemPriceRefresh.Id == dbContext.ItemPriceRefresh.Max(refreshMax => refreshMax.Id))
            .Include(itemPriceDbModel => itemPriceDbModel.ItemPriceRefresh)
            .ToList();
        
        var itemsInList = dbContext.ItemListItemAction.Where(item => item.List.Id == list.Id)
            .GroupBy(item => item.ItemId).ToList();
        var steamValues = new List<decimal>();
        var buffValues = new List<decimal>();

        foreach (var item in itemsInList)
        {
            var itemPrice = latestItemPrices.FirstOrDefault(price => price.ItemId == item.Key);
            if (itemPrice is null)
            {
                continue;
            }
            
            var actions = item.ToList();
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

            switch (list.Currency)
            {
                case "EUR":
                {
                    if (itemPrice.SteamPriceEur is not null)
                    {
                        steamValues.Add(itemPrice.SteamPriceEur.Value * totalItemCount);
                    }

                    if (itemPrice.BuffPriceEur is not null)
                    {
                        buffValues.Add(itemPrice.BuffPriceEur.Value * totalItemCount);
                    }

                    break;
                }
                case "USD":
                {
                    if (itemPrice.SteamPriceUsd is not null)
                    {
                        steamValues.Add(itemPrice.SteamPriceUsd.Value * totalItemCount);
                    }

                    if (itemPrice.BuffPriceUsd is not null)
                    {
                        buffValues.Add(itemPrice.BuffPriceUsd.Value * totalItemCount);
                    }

                    break;
                }
            }
        }

        
        decimal? steamValueResult = steamValues.Count != 0 ? steamValues.Sum() : null;
        decimal? buffValueResult = buffValues.Count != 0 ? buffValues.Sum() : null;
        var listValue = new ItemListValueDbModel
        {
            List = list,
            SteamValue = steamValueResult,
            BuffValue = buffValueResult,
            ItemPriceRefresh = latestItemPrices.FirstOrDefault()?.ItemPriceRefresh,
            CreatedUtc = DateTime.UtcNow
        };
        var newItemListValue = await dbContext.ItemListValues.AddAsync(listValue);
        await dbContext.SaveChangesAsync();
        return newItemListValue.Entity;
    }
}