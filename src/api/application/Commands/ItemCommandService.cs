using ErrorOr;
using infrastructure.Items;
using shared.Models;

namespace application.Commands;

public class ItemCommandService
{
    private readonly ItemsService _itemsService;

    public ItemCommandService(ItemsService itemsService)
    {
        _itemsService = itemsService;
    }

    public ErrorOr<IEnumerable<ItemSearchResponse>> Search(string searchString)
    {
        var search = _itemsService.Search(searchString);
        if (search.IsError)
        {
            return search.FirstError;
        }

        return search.Value.Select(model => new ItemSearchResponse(model.Id, model.Name, model.Image)).ToList();
    }
}