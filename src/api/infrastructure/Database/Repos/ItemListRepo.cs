using ErrorOr;
using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using Throw;

namespace infrastructure.Database.Repos;

public class ItemListRepo(XDbContext dbContext)
{
    public Task<List<ItemListDbModel>> GetAllListsForUser(string userId)
    {
        return Task.FromResult(dbContext.Lists.Where(list => list.Deleted == false && list.UserId.Equals(userId))
            .ToList());
    }

    public async
        Task<(
            ItemListDbModel List,
            List<ItemListSnapshotDbModel> Snapshots,
            List<ItemListItemActionDbModel> ItemActions,
            ItemPriceRefreshDbModel LastPriceRefresh,
            List<ItemPriceDbModel> PricesForItemsInList
            )> GetListInfos(
            long listId)
    {
        var list = await dbContext.Lists.FindAsync(listId);
        list.ThrowIfNull().Throw().IfTrue(list.Deleted);
        var snapshots = dbContext.ListSnapshots.Where(snapshot => snapshot.List.Id == listId).ToList();
        var itemAction = dbContext.ItemActions.Where(action => action.List.Id == listId).ToList();
        var lastPriceRefresh = await dbContext.PricesRefresh.OrderByDescending(priceRefresh => priceRefresh.CreatedUtc).FirstAsync();
        var itemsInListIds = itemAction.GroupBy(action => action.ItemId).Select(group => group.Key);
        var pricesForItemsInList = dbContext.Prices.Where(price =>
            price.ItemPriceRefresh.Id == lastPriceRefresh.Id && itemsInListIds.Contains(price.ItemId)).ToList();

        return (list, snapshots, itemAction, lastPriceRefresh, pricesForItemsInList);
    }

    public async Task<bool> ExistsWithNameForUser(string userId, string listName)
    {
        return await dbContext.Lists.AnyAsync(list =>
            list.Deleted == false && list.UserId.Equals(userId) && list.Name.Equals(listName));
    }

    public async Task<ErrorOr<ItemListDbModel>> GetByUrl(string url)
    {
        var list = await dbContext.Lists.FirstOrDefaultAsync(list => list.Deleted == false && list.Url.Equals(url));
        if (list is null)
        {
            return Error.NotFound(description: "No list found for the given url");
        }

        return list;
    }

    public async Task<ItemListDbModel> CreateNewList(
        string userId,
        string listName,
        string? listDescription,
        string currency,
        bool makeListPublic)
    {
        var url = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        // Replace URL unfriendly characters
        url = url
            .Replace("=", "")
            .Replace("/", "_")
            .Replace("+", "-");

        var currentDateTimeUtc = DateTime.UtcNow;
        var itemList = await dbContext.Lists.AddAsync(new ItemListDbModel
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
        return itemList.Entity;
    }

    public async Task DeleteList(long listId)
    {
        var listToRemove = await dbContext.Lists.FirstAsync(list => list.Id == listId);
        listToRemove.Deleted = true;
    }

    public async Task<int> GetCurrentItemCount(ItemListDbModel list, long itemId)
    {
        var actionsForItemId = dbContext.ItemActions
            .Where(action => action.List.Id == list.Id && action.ItemId == itemId).OrderBy(action => action.CreatedUtc);
        var itemCount = 0;
        foreach (var action in actionsForItemId)
        {
            if (action.Action.Equals("B"))
            {
                itemCount += action.Amount;
            }
            else if (action.Action.Equals("S"))
            {
                itemCount -= action.Amount;
            }
        }

        if (itemCount < 0)
        {
            throw new Exception($"Item count cant be negative. ItemCount: {itemCount}");
        }

        return await Task.FromResult(itemCount);
    }

    public async Task AddItemAction(string actionType,
        ItemListDbModel list,
        long itemId,
        long unitPrice,
        int amount)
    {
        if (string.IsNullOrWhiteSpace(actionType) ||
            (actionType.Equals("B") == false && actionType.Equals("S") == false))
        {
            throw new Exception($"Action type \"{actionType}\" is not valid");
        }

        var currentDate = DateTime.UtcNow;
        var listItem = new ItemListItemActionDbModel
        {
            List = list,
            ItemId = itemId,
            Action = actionType,
            UnitPrice = unitPrice,
            Amount = amount,
            CreatedUtc = currentDate
        };
        await dbContext.ItemActions.AddAsync(listItem);
    }

    public async Task DeleteItemAction(ItemListDbModel list, long itemActionId)
    {
        var actionToDelete =
            await dbContext.ItemActions.FirstAsync(action => action.Id == itemActionId && action.List.Id == list.Id);
        dbContext.ItemActions.Remove(actionToDelete);
    }

    public async Task UpdateName(long listId, string newListName)
    {
        await dbContext.Lists
            .Where(l => l.Id == listId)
            .ExecuteUpdateAsync(b =>
                b.SetProperty(l => l.Name, newListName)
            );
    }

    public async Task UpdateDescription(long listId, string newDescription)
    {
        await dbContext.Lists
            .Where(l => l.Id == listId)
            .ExecuteUpdateAsync(b =>
                b.SetProperty(l => l.Description, newDescription)
            );
    }

    public async Task UpdatePublic(long listId, bool newPublic)
    {
        await dbContext.Lists
            .Where(l => l.Id == listId)
            .ExecuteUpdateAsync(b =>
                b.SetProperty(l => l.Public, newPublic)
            );
    }

    public async Task<ItemListItemActionDbModel> GetItemActionById(long actionId)
    {
        return await dbContext.ItemActions.FirstAsync(action => action.Id == actionId);
    }
}