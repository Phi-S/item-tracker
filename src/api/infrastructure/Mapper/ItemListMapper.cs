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
        foreach (var itemListItemActionsGroup in itemListItemActions.GroupBy(action => action.ItemId))
        {
            var itemInfoResult = itemsService.GetById(itemListItemActionsGroup.Key);
            if (itemInfoResult.IsError)
            {
                return itemInfoResult.FirstError;
            }

            var itemInfo = itemInfoResult.Value;
            var itemActions = new List<ListItemActionResponse>();
            var buyActions = new List<ListItemActionResponse>();
            var sellActions = new List<ListItemActionResponse>();
            foreach (var itemListItemAction in itemListItemActionsGroup)
            {
                var action = new ListItemActionResponse(
                    itemListItemAction.Id,
                    itemListItemAction.Action,
                    itemListItemAction.Amount,
                    itemListItemAction.PricePerOne,
                    itemListItemAction.CreatedUtc
                );
                itemActions.Add(action);
                if (itemListItemAction.Action.Equals("B"))
                {
                    buyActions.Add(action);
                }
                else if (itemListItemAction.Action.Equals("S"))
                {
                    sellActions.Add(action);
                }
            }

            var totalBuyAmount = buyActions.Sum(action => action.Amount);
            var totalBuyPrice = buyActions.Sum(action => action.Amount * action.PricePerOne);
            var averageBuyPrice = totalBuyAmount == 0 ? 0 : totalBuyPrice / totalBuyAmount;

            var totalSellAmount = sellActions.Sum(action => action.Amount);
            var totalSellPrice = buyActions.Sum(action => action.Amount * action.PricePerOne);
            var averageSellPrice = totalSellAmount == 0 ? 0 : totalSellPrice / totalSellAmount;

            var item = new ListItemResponse(
                itemInfo.Id,
                itemInfo.Name,
                itemInfo.Image,
                totalBuyAmount,
                totalBuyPrice,
                averageBuyPrice,
                totalSellAmount,
                totalSellPrice,
                averageSellPrice,
                itemActions
            );
            items.Add(item);
        }

        var listValues = new List<ListValueResponse>();
        foreach (var itemListValue in itemListValues)
        {
            var listValue = new ListValueResponse(
                itemListValue.SteamValue,
                itemListValue.BuffValue,
                itemListValue.InvestedCapital,
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