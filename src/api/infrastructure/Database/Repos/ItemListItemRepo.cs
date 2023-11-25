using infrastructure.Database.Models;

namespace infrastructure.Database.Repos;

public class ItemListItemRepo(XDbContext dbContext)
{
    public async Task<List<ItemListItemActionDbModel>> GetItemsForList(ItemListDbModel listDbModel)
    {
        return await Task.FromResult(dbContext.ItemListItemAction.Where(item => item.ItemListDbModel.Id == listDbModel.Id).ToList());
    }

    public async Task<ItemListItemActionDbModel> Buy(ItemListDbModel itemListDbModel, long itemId, decimal pricePerOne, long amount)
    {
        var currentDate = DateTime.UtcNow;
        var listItem = new ItemListItemActionDbModel
        {
            ItemListDbModel = itemListDbModel,
            ItemId = itemId,
            Action = "B",
            Amount = amount,
            PricePerOne = pricePerOne,
            CreatedUtc = currentDate
        };
        var addedItem = await dbContext.ItemListItemAction.AddAsync(listItem);
        await dbContext.SaveChangesAsync();
        return addedItem.Entity;
    }

    public async Task<ItemListItemActionDbModel> Sell(ItemListDbModel itemListDbModel, long itemId, decimal pricePerOne, long amount)
    {
        var currentDate = DateTime.UtcNow;
        var listItem = new ItemListItemActionDbModel
        {
            ItemListDbModel = itemListDbModel,
            ItemId = itemId,
            Action = "S",
            PricePerOne = pricePerOne,
            Amount = amount,
            CreatedUtc = currentDate
        };
        var addedItem = await dbContext.ItemListItemAction.AddAsync(listItem);
        await dbContext.SaveChangesAsync();
        return addedItem.Entity;
    }

    public async Task DeleteItemAction(long itemActionId)
    {
        var itemActionToDelete = dbContext.ItemListItemAction.Where(action => action.Id == itemActionId);
        dbContext.ItemListItemAction.RemoveRange(itemActionToDelete);
        await dbContext.SaveChangesAsync();
    }
}