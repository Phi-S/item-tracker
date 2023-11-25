using infrastructure.Database.Models;

namespace infrastructure.Database.Repos;

public class ItemPriceRepo(XDbContext dbContext)
{
    public async Task Add(List<ItemPriceDbModel> itemPrices)
    {
        await dbContext.ItemPrices.AddRangeAsync(itemPrices);
        await dbContext.SaveChangesAsync();
    }
}