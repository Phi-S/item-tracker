using ErrorOr;
using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using Throw;

namespace infrastructure.Database.Repos;

public class ItemListRepo
{
    private readonly XDbContext _dbContext;

    public ItemListRepo(XDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    #region List

    public async Task<ItemListDbModel> CreateNewList(
        string userId,
        string url,
        string listName,
        string? listDescription,
        string currency,
        bool makeListPublic)
    {
        var currentDateTimeUtc = DateTime.UtcNow;
        var itemList = await _dbContext.Lists.AddAsync(new ItemListDbModel
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

    public async Task UpdateListName(long listId, string newListName)
    {
        var list = await _dbContext.Lists.FindAsync(listId);
        list.ThrowIfNull();
        list.Name = newListName;
    }

    public async Task UpdateListDescription(long listId, string newDescription)
    {
        var list = await _dbContext.Lists.FindAsync(listId);
        list.ThrowIfNull();
        list.Description = newDescription;
    }

    public async Task UpdateListPublicState(long listId, bool newPublic)
    {
        var list = await _dbContext.Lists.FindAsync(listId);
        list.ThrowIfNull();
        list.Public = newPublic;
    }

    public async Task DeleteList(long listId)
    {
        var listToRemove = await _dbContext.Lists.FirstAsync(list => list.Id == listId);
        listToRemove.Deleted = true;
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
        var list = await _dbContext.Lists.FindAsync(listId);
        list.ThrowIfNull().Throw().IfTrue(list.Deleted);
        var snapshots = _dbContext.ListSnapshots.Where(snapshot => snapshot.List.Id == listId).ToList();
        var itemAction = _dbContext.ItemActions.Where(action => action.List.Id == listId).ToList();
        var lastPriceRefresh = await _dbContext.PricesRefresh.OrderByDescending(priceRefresh => priceRefresh.CreatedUtc)
            .FirstAsync();
        var itemsInListIds = itemAction.GroupBy(action => action.ItemId).Select(group => group.Key);
        var pricesForItemsInList = _dbContext.Prices.Where(price =>
            price.ItemPriceRefresh.Id == lastPriceRefresh.Id && itemsInListIds.Contains(price.ItemId)).ToList();

        return (list, snapshots, itemAction, lastPriceRefresh, pricesForItemsInList);
    }

    public Task<List<ItemListDbModel>> GetAllListsForUser(string userId)
    {
        return Task.FromResult(_dbContext.Lists.Where(list => list.Deleted == false && list.UserId.Equals(userId))
            .ToList());
    }

    public Task<bool> ListNameTakenForUser(string userId, string listName)
    {
        return _dbContext.Lists.AnyAsync(list =>
            list.Deleted == false && list.UserId.Equals(userId) && list.Name.Equals(listName));
    }

    public async Task<ErrorOr<ItemListDbModel>> GetListByUrl(string url)
    {
        var list = await _dbContext.Lists.FirstOrDefaultAsync(list => list.Deleted == false && list.Url.Equals(url));
        if (list is null)
        {
            return Error.NotFound(description: "No list found for the given url");
        }

        return list;
    }

    #endregion


    public Task<int> GetListItemCount(long listId, long itemId)
    {
        var actionsForItemId = _dbContext.ItemActions
            .Where(action => action.List.Id == listId && action.ItemId == itemId).OrderBy(action => action.CreatedUtc);
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

        return Task.FromResult(itemCount);
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
        await _dbContext.ItemActions.AddAsync(listItem);
    }

    public async Task DeleteItemAction(ItemListDbModel list, long itemActionId)
    {
        var actionToDelete =
            await _dbContext.ItemActions.FirstAsync(action => action.Id == itemActionId && action.List.Id == list.Id);
        _dbContext.ItemActions.Remove(actionToDelete);
    }

    public Task<ItemListItemActionDbModel> GetItemActionById(long actionId)
    {
        return _dbContext.ItemActions.FirstAsync(action => action.Id == actionId);
    }

    public async Task NewSnapshot(ItemListDbModel list, ItemPriceRefreshDbModel priceRefresh)
    {
        await _dbContext.ListSnapshots.AddAsync(new ItemListSnapshotDbModel
        {
            List = list,
            ItemPriceRefresh = priceRefresh,
            CreatedUtc = DateTime.UtcNow
        });
    }
}