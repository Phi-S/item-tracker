using infrastructure.Database.Models;

namespace infrastructure.Database.Repos;

public class ItemPriceRepo(XDbContext dbContext)
{
    public async Task<ItemPriceRefreshDbModel> CreateNew()
    {
        var newItemPriceRefresh = await dbContext.ItemPriceRefresh.AddAsync(new ItemPriceRefreshDbModel()
        {
            CreatedUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
        return newItemPriceRefresh.Entity;
    }

    public async Task Add(List<ItemPriceDbModel> itemPrices)
    {
        await dbContext.ItemPrices.AddRangeAsync(itemPrices);
        await dbContext.SaveChangesAsync();
    }
}