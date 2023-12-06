using infrastructure.Database.Models;

namespace infrastructure.Database.Repos;

public class ItemPriceRepo(XDbContext dbContext)
{
    public async Task<ItemPriceRefreshDbModel> CreateNew(
        double eurToUsdExchangeRate,
        DateTime steamPricesLastModified,
        DateTime buff163PricesLastModified)
    {
        var newItemPriceRefresh = await dbContext.PricesRefresh.AddAsync(
            new ItemPriceRefreshDbModel
            {
                EurToUsdExchangeRate = eurToUsdExchangeRate,
                SteamPricesLastModified = steamPricesLastModified,
                Buff163PricesLastModified = buff163PricesLastModified,
                CreatedUtc = DateTime.UtcNow
            });
        return newItemPriceRefresh.Entity;
    }
    
    public async Task Add(IEnumerable<ItemPriceDbModel> itemPrices)
    {
        await dbContext.Prices.AddRangeAsync(itemPrices);
    }
}