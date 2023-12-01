using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Database.Repos;

public class ItemListRepo(XDbContext dbContext)
{
    public async Task<List<Tuple<ItemListDbModel, List<ItemListValueDbModel>, List<ItemListItemActionDbModel>>>>
        GetListInfosForUserId(string userId)
    {
        var result =
            dbContext.ItemLists.Where(list => list.Deleted == false && list.UserId.Equals(userId))
                .Select(list => Tuple.Create(
                    list,
                    dbContext.ItemListValues.Where(value => value.List.Id == list.Id).ToList(),
                    dbContext.ItemListItemAction.Where(item => item.List.Id == list.Id).ToList()
                )).ToList();
        return await Task.FromResult(result);
    }

    public (ItemListDbModel list, List<ItemListValueDbModel> listValues, List<ItemListItemActionDbModel> items)
        GetListInfos(
            string listUrl)
    {
        var result =
            dbContext.ItemLists.Where(list => list.Deleted == false && list.Url.Equals(listUrl))
                .Select(list => Tuple.Create(
                    list,
                    dbContext.ItemListValues.Where(value => value.List.Id == list.Id).ToList(),
                    dbContext.ItemListItemAction.Where(item => item.List.Id == list.Id).ToList()
                )).Take(1).First();
        return (result.Item1, result.Item2, result.Item3);
    }

    public async Task<bool> ExistsWithNameForUser(string userId, string listName)
    {
        return await dbContext.ItemLists.AnyAsync(list =>
            list.Deleted == false && list.UserId.Equals(userId) && list.Name.Equals(listName));
    }

    public async Task<ItemListDbModel> GetByUrl(string url)
    {
        return await dbContext.ItemLists.FirstAsync(list => list.Deleted == false && list.Url.Equals(url));
    }

    public Task<IEnumerable<ItemListDbModel>> GetAllForUserSub(string userId)
    {
        return Task.FromResult<IEnumerable<ItemListDbModel>>(dbContext.ItemLists.Where(list =>
            list.Deleted == false && list.UserId.Equals(userId)));
    }

    public async Task<bool> ListNameExists(string userId, string listName)
    {
        return await dbContext.ItemLists.AnyAsync(list =>
            list.Deleted == false && list.UserId.Equals(userId) && list.Name.Equals(listName));
    }

    public async Task<ItemListDbModel> New(
        string userId,
        string listName,
        string? listDescription,
        string currency,
        bool makeListPublic)
    {
        var url = GenerateNewUrl();
        var currentDateTimeUtc = DateTime.UtcNow;
        var itemList = await dbContext.ItemLists.AddAsync(new ItemListDbModel
        {
            UserId = userId,
            Name = listName,
            Description = string.IsNullOrWhiteSpace(listDescription) ? null : listDescription,
            Url = url,
            Currency = currency,
            Public = makeListPublic,
            Deleted = false,
            UpdatedUtc = currentDateTimeUtc,
            CreatedUtc = currentDateTimeUtc
        });
        await dbContext.SaveChangesAsync();
        return itemList.Entity;
    }

    public async Task Delete(long listId)
    {
        var listToRemove = await dbContext.ItemLists.FirstAsync(list => list.Id == listId);
        listToRemove.Deleted = true;
        await dbContext.SaveChangesAsync();
    }

    public async Task<ItemListDbModel> UpdateName(long listId, string newListName)
    {
        var list = await dbContext.ItemLists.FirstAsync(itemList => itemList.Id == listId);
        if (list.Name.Equals(newListName))
        {
            return list;
        }

        list.Name = newListName;
        list.UpdatedUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return list;
    }

    public async Task<ItemListDbModel> UpdateDescription(long listId, string newDescription)
    {
        var list = await dbContext.ItemLists.FirstAsync(itemList => itemList.Id == listId);
        if (!string.IsNullOrWhiteSpace(list.Description) && list.Description.Equals(newDescription))
        {
            return list;
        }

        list.Description = newDescription;
        list.UpdatedUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return list;
    }

    public async Task<ItemListDbModel> UpdatePublic(long listId, bool makeListPublic)
    {
        var list = await dbContext.ItemLists.FirstAsync(itemList => itemList.Id == listId);
        if (list.Public == makeListPublic)
        {
            return list;
        }

        list.Public = makeListPublic;
        list.UpdatedUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return list;
    }

    private static string GenerateNewUrl()
    {
        var url = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        // Replace URL unfriendly characters
        url = url
            .Replace("=", "")
            .Replace("/", "_")
            .Replace("+", "-");

        // Remove the trailing ==
        return url;
    }

    public async Task<List<ItemListItemActionDbModel>> GetItemsForList(ItemListDbModel listDbModel)
    {
        return await Task.FromResult(dbContext.ItemListItemAction
            .Where(item => item.List.Id == listDbModel.Id).ToList());
    }

    public async Task<ItemListItemActionDbModel> BuyItem(ItemListDbModel itemListDbModel, long itemId,
        decimal pricePerOne,
        long amount)
    {
        var currentDate = DateTime.UtcNow;
        var listItem = new ItemListItemActionDbModel
        {
            List = itemListDbModel,
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

    public async Task<ItemListItemActionDbModel> SellItem(ItemListDbModel itemListDbModel, long itemId,
        decimal pricePerOne,
        long amount)
    {
        var currentDate = DateTime.UtcNow;
        var listItem = new ItemListItemActionDbModel
        {
            List = itemListDbModel,
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

    public async Task DeleteItem(long itemActionId)
    {
        var itemActionToDelete = await dbContext.ItemListItemAction.FirstAsync(action => action.Id == itemActionId);
        dbContext.ItemListItemAction.Remove(itemActionToDelete);
        await dbContext.SaveChangesAsync();
    }
}