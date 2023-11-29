using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using shared.Models.ListResponse;

namespace infrastructure.Database.Repos;

public class ItemListRepo(XDbContext dbContext)
{
    public record AllResult(
        ItemListDbModel List,
        IEnumerable<ItemListValueDbModel> Values,
        IEnumerable<ItemListItemActionDbModel> Items
    );

    public List<Tuple<ItemListDbModel, List<ItemListValueDbModel>, List<ItemListItemActionDbModel>>> All(
        string userId)
    {
        var result =
            dbContext.ItemLists.Where(list => list.Deleted == false && list.UserId.Equals(userId))
                .Select(list => Tuple.Create(
                    list,
                    dbContext.ItemListValues.Where(value => value.ItemListDbModel.Id == list.Id).ToList(),
                    dbContext.ItemListItemAction.Where(item => item.ItemListDbModel.Id == list.Id).ToList()
                )).ToList();
        return result;
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
        return dbContext.ItemLists.First(list => list.Url.Equals(url));
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

    public static string GenerateNewUrl()
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
}