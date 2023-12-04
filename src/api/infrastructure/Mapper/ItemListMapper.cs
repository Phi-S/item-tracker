using ErrorOr;
using infrastructure.Database.Models;
using infrastructure.Items;
using shared.Models.ListResponse;

namespace infrastructure.Mapper;

public static class ItemListMapper
{
    public static ErrorOr<ListResponse> MapToListResponse(
        ItemListDbModel itemListDbModel,
        List<ItemListSnapshotDbModel> itemListValues,
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
            var buyPrices = new List<decimal>();
            var sellActions = new List<ListItemActionResponse>();
            var currentAmountInvested = 0;
            foreach (var itemAction in itemListItemActionsGroup.OrderBy(action => action.CreatedUtc))
            {
                var actionResponse = new ListItemActionResponse(
                    itemAction.Id,
                    itemAction.Action,
                    itemAction.Amount,
                    itemAction.PricePerOne,
                    itemAction.CreatedUtc
                );
                itemActions.Add(actionResponse);
                if (itemAction.Action.Equals("B"))
                {
                    // Clears buyPrices so all actions before the entire amount of one item is sold is not used to calculate the averageBuyPrice
                    // Example: if not corrected:
                    // buy 5 for 1; avg buy price 1;
                    // sell 5 fox x; avg buy price 1;
                    // buy 5 for 2; avg buy price == 1.5 / because 15 / 10 insted of 10 / 5
                    // corrected:
                    // buy 5 for 1; avg buy price 1;
                    // sell 5 fox x; avg buy price 0;
                    // buy 5 for 2; avg buy price == 2
                    if (currentAmountInvested == 0)
                    {
                        buyPrices.Clear();
                    }

                    buyPrices.AddRange(Enumerable.Repeat(actionResponse.Price, actionResponse.Amount));
                    buyActions.Add(actionResponse);
                    currentAmountInvested += actionResponse.Amount;
                }
                else if (itemAction.Action.Equals("S"))
                {
                    sellActions.Add(actionResponse);
                    currentAmountInvested -= actionResponse.Amount;
                }
            }

            var currentAverageBuyPrice = buyPrices.Average();
            var currentCapitalInvested = currentAverageBuyPrice * currentAmountInvested;

            var item = new ListItemResponse(
                itemInfo.Id,
                itemInfo.Name,
                itemInfo.Image,
                currentCapitalInvested,
                currentAmountInvested,
                currentAverageBuyPrice,
                itemActions
            );
            items.Add(item);
        }

        var listValues = new List<ListSnapshotResponse>();
        foreach (var itemListValue in itemListValues)
        {
            var listValue = new ListSnapshotResponse(
                itemListValue.InvestedCapital,
                itemListValue.ItemCount,
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