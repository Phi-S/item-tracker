using infrastructure.Database.Models;

namespace infrastructure.Database.Repos;

public class ItemPriceRepo(XDbContext dbContext)
{
    public async Task<ItemPriceRefreshDbModel> CreateNew(DateTime steamPricesLastModified, DateTime buff163PricesLastModified)
    {
        var newItemPriceRefresh = await dbContext.ItemPriceRefresh.AddAsync(
            new ItemPriceRefreshDbModel
        {
            SteamPricesLastModified = steamPricesLastModified,
            Buff163PricesLastModified = buff163PricesLastModified,
            CreatedUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return newItemPriceRefresh.Entity;
    }

    public async Task Add(IEnumerable<ItemPriceDbModel> itemPrices)
    {
        await dbContext.ItemPrices.AddRangeAsync(itemPrices);
        await dbContext.SaveChangesAsync();
    }
}