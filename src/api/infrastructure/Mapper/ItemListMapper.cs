using ErrorOr;
using infrastructure.Database.Models;
using infrastructure.Items;
using shared.Models.ListResponse;

namespace infrastructure.Mapper;

public static class ItemListMapper
{
    public static ErrorOr<ListResponse> MapToListResponse(
        ItemListDbModel itemListDbModel,
        List<ItemListValueDbModel> itemListValues,
        List<ItemListItemActionDbModel> itemListItemActions,
        ItemsService itemsService)
    {
        var items = new List<ListItemResponse>();
        foreach (var itemListItemAction in itemListItemActions)
        {
            var itemInfo = itemsService.GetById(itemListItemAction.ItemId);
            if (itemInfo.IsError)
            {
                return itemInfo.FirstError;
            }

            var item = new ListItemResponse(
                itemListItemAction.Id,
                itemListItemAction.ItemId,
                itemInfo.Value.Name,
                itemInfo.Value.Image,
                itemListItemAction.Action,
                itemListItemAction.PricePerOne,
                itemListItemAction.Amount,
                itemListItemAction.CreatedUtc);
            items.Add(item);
        }

        var listValues = new List<ListValueResponse>();
        foreach (var itemListValue in itemListValues)
        {
            var listValue = new ListValueResponse(
                itemListValue.SteamValue,
                itemListValue.BuffValue,
                itemListValue.CreatedUtc
            );
            listValues.Add(listValue);
        }

        var listResponse = new ListResponse(
            itemListDbModel.Name,
            itemListDbModel.Description,
            itemListDbModel.Url,
            itemListDbModel.Currency,
            itemListDbModel.Public,
            itemListDbModel.UserId,
            items,
            listValues);

        return listResponse;
    }
}