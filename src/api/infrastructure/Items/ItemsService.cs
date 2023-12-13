using System.Reflection;
using System.Text;
using System.Text.Json;
using ErrorOr;

namespace infrastructure.Items;

public class ItemsService
{
    private readonly List<ItemModel> _itemList;

    public ItemsService()
    {
        var itemsJsonStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("infrastructure.Items.items.json");
        if (itemsJsonStream is null)
        {
            throw new Exception("Failed to get \"infrastructure.itemList.items.json\" file");
        }

        using var reader = new StreamReader(itemsJsonStream, Encoding.UTF8);
        var itemsJson = reader.ReadToEnd();
        var itemList = JsonSerializer.Deserialize<List<ItemModel>>(itemsJson);
        _itemList = itemList ?? throw new Exception("Failed to get Deserialize items.json");
    }

    public ErrorOr<List<ItemModel>> GetAll()
    {
        return _itemList;
    }

    public ErrorOr<ItemModel> GetById(long itemId)
    {
        var item = _itemList.FirstOrDefault(model => model.Id == itemId);
        if (item is null)
        {
            return Error.NotFound(description: $"Failed to find item with the id {itemId}");
        }

        return item;
    }

    public ErrorOr<List<ItemModel>> Search(string searchString)
    {
        var searchStringWords = searchString.ToLower().Split();
        var searchForPriorityMatches = searchStringWords.Length >= 2;

        var matches = new Dictionary<ItemModel, int>();
        foreach (var item in _itemList)
        {
            var name = item.Name.ToLower();
            if (name.Equals(searchString))
            {
                return new List<ItemModel> { item };
            }

            if (searchForPriorityMatches && name.Contains(searchString))
            {
                matches.Add(item, int.MaxValue);
                continue;
            }

            var itemNameWords = name.Split();
            var intersect = searchStringWords.Intersect(itemNameWords).ToList();
            if (intersect.Count > 0)
            {
                matches.Add(item, intersect.Count);
                continue;
            }

            foreach (var searchStringWord in searchStringWords)
            {
                if (name.Contains(searchStringWord))
                {
                    matches.Add(item,int.MinValue);
                    break;
                }
            }
        }

        const int maxSearchResultCount = 15;
        var result = matches
            .OrderByDescending(match => match.Value)
            .Select(match => match.Key)
            .Take(maxSearchResultCount);

        return result.ToList();
    }
}