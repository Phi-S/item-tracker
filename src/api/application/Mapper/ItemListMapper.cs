using infrastructure.Database.Models;
using infrastructure.Items;
using shared.Models.ListResponse;

namespace application.Mapper;

public static class ItemListMapper
{
    #region MapListItemresponses

    private static Task<List<ListItemResponse>> MapListItemResponses(
        List<ItemListItemActionDbModel> itemListItemActions, ItemsService itemsService)
    {
        var result = new List<ListItemResponse>();
        foreach (var itemListItemActionsGroup in itemListItemActions.GroupBy(action => action.ItemId))
        {
            var itemInfoResult = itemsService.GetById(itemListItemActionsGroup.Key);
            if (itemInfoResult.IsError)
            {
                throw new Exception(itemInfoResult.FirstError.Description);
            }

            var item = MapListItemResponse(itemInfoResult.Value, itemListItemActionsGroup.ToList());
            result.Add(item);
        }

        return Task.FromResult(result);
    }

    public static ListItemResponse MapListItemResponse(ItemModel item, List<ItemListItemActionDbModel> itemActions)
    {
        var itemActionResponses = new List<ListItemActionResponse>();
        var buyPrices = new List<long>();
        long salesValue = 0;
        long profit = 0;
        var amountInvested = 0;
        var gotSales = false;
        foreach (var itemAction in itemActions.OrderBy(action => action.CreatedUtc))
        {
            var actionResponse = new ListItemActionResponse(
                itemAction.Id,
                itemAction.Action,
                itemAction.Amount,
                itemAction.UnitPrice,
                itemAction.CreatedUtc
            );
            itemActionResponses.Add(actionResponse);
            if (actionResponse.Action.Equals("B"))
            {
                // Clears buyPrices so all actions before the entire amount of one item is sold is not used to calculate the averageBuyPrice
                // Example: if not corrected:
                // buy 5 for 1; avg buy price 1;
                // sell 5 fox x; avg buy price 1;
                // buy 5 for 2; avg buy price == 1.5 / because 15 / 10 instead of 10 / 5
                // corrected:
                // buy 5 for 1; avg buy price 1;
                // sell 5 fox x; avg buy price 0;
                // buy 5 for 2; avg buy price == 2
                if (amountInvested == 0)
                {
                    buyPrices.Clear();
                }

                buyPrices.AddRange(Enumerable.Repeat(actionResponse.Price, actionResponse.Amount));
                amountInvested += actionResponse.Amount;
            }
            else if (actionResponse.Action.Equals("S"))
            {
                gotSales = true;
                salesValue += actionResponse.Amount * actionResponse.Price;
                var tempAverageBuyPrice = (long)Math.Round(buyPrices.Average(), 0);
                profit += (actionResponse.Price - tempAverageBuyPrice) * actionResponse.Amount;
                amountInvested -= actionResponse.Amount;
            }
        }

        var averageBuyPrice = buyPrices.Count == 0 ? 0 : (long)Math.Round(buyPrices.Average(), 0);
        var capitalInvested = gotSales ? averageBuyPrice * amountInvested : buyPrices.Sum();

        var listItemResponse = new ListItemResponse(
            item.Id,
            item.Name,
            item.Image,
            capitalInvested,
            amountInvested,
            averageBuyPrice,
            salesValue,
            profit,
            itemActionResponses
        );
        return listItemResponse;
    }

    #endregion

    #region ListSnapshotResponse

    private static ListSnapshotResponse ListSnapshotResponse(ItemListSnapshotDbModel itemListSnapshot,
        List<ItemListItemActionDbModel> listActions)
    {
        long totalInvestedCapital = 0;
        long totalItemCount = 0;
        long salesValue = 0;
        long profit = 0;
        var actionsGroupedByItem = listActions.GroupBy(action => action.ItemId);
        foreach (var actionItemGroup in actionsGroupedByItem)
        {
            var buyPrices = new List<long>();
            var itemCount = 0;
            var actions = actionItemGroup
                .Where(action => action.CreatedUtc <= itemListSnapshot.CreatedUtc)
                .OrderBy(action => action.CreatedUtc);
            foreach (var action in actions)
            {
                if (action.Action.Equals("B"))
                {
                    if (itemCount == 0)
                    {
                        buyPrices.Clear();
                    }

                    buyPrices.AddRange(Enumerable.Repeat(action.UnitPrice, action.Amount));
                    itemCount += action.Amount;
                }
                else if (action.Action.Equals("S"))
                {
                    salesValue += action.UnitPrice * action.Amount;
                    profit += (long)Math.Round((action.UnitPrice - buyPrices.Average()) * action.Amount, 0);
                    itemCount -= action.Amount;
                }
            }

            totalInvestedCapital += buyPrices.Count == 0 ? 0 : (long)Math.Round(buyPrices.Average() * itemCount, 0);
            totalItemCount += itemCount;
        }

        return new ListSnapshotResponse(
            totalInvestedCapital,
            totalItemCount,
            salesValue,
            profit,
            itemListSnapshot.SteamValue,
            itemListSnapshot.BuffValue,
            itemListSnapshot.CreatedUtc
        );
    }

    private static Task<List<ListSnapshotResponse>> MapListSnapshotResponses(
        IReadOnlyCollection<ItemListSnapshotDbModel> listSnapshots,
        List<ItemListItemActionDbModel> listActions)
    {
        var result = new List<ListSnapshotResponse>();
        if (listSnapshots.Count == 0)
        {
            return Task.FromResult(result);
        }

        if (listActions.Count == 0)
        {
            foreach (var snapshot in listSnapshots)
            {
                result.Add(new ListSnapshotResponse(0, 0, 0, 0, 0, 0, snapshot.CreatedUtc));
            }

            return Task.FromResult(result);
        }


        foreach (var snapshot in listSnapshots.OrderBy(value => value.CreatedUtc))
        {
            result.Add(ListSnapshotResponse(snapshot, listActions));
        }

        return Task.FromResult(result);
    }

    #endregion


    public static async Task<ListResponse> MapToListResponse(
        ItemListDbModel itemListDbModel,
        List<ItemListSnapshotDbModel> itemListSnapshots,
        List<ItemListItemActionDbModel> itemListItemActions,
        ItemsService itemsService)
    {
        var mapListItemResponsesTask = MapListItemResponses(itemListItemActions, itemsService);
        var mapListSnapshotResponsesTask = MapListSnapshotResponses(itemListSnapshots, itemListItemActions);
        await Task.WhenAll(mapListItemResponsesTask, mapListSnapshotResponsesTask);

        var listResponse = new ListResponse(
            itemListDbModel.Name,
            itemListDbModel.Description,
            itemListDbModel.Url,
            itemListDbModel.Currency,
            itemListDbModel.Public,
            itemListDbModel.UserId,
            mapListItemResponsesTask.Result,
            mapListSnapshotResponsesTask.Result);

        return listResponse;
    }
}