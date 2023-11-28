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

    public ErrorOr<ItemModel> GetByName(string itemName)
    {
        var item = _itemList.FirstOrDefault(model => model.Name.Equals(itemName));
        if (item is null)
        {
            return Error.NotFound(description: $"Failed to find item with the name {itemName}");
        }

        return item;
    }

    public ErrorOr<List<ItemModel>> Search(string searchString)
    {
        return _itemList.Where(model => model.Name.Contains(searchString, StringComparison.CurrentCultureIgnoreCase))
            .Take(10).ToList();
    }
}