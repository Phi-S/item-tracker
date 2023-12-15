using System.Runtime.InteropServices.JavaScript;
using ErrorOr;
using infrastructure.Database.Models;
using infrastructure.ExchangeRates;
using infrastructure.Items;
using shared.Currencies;
using shared.Models.ListResponse;

namespace application.Mapper;

public static class ItemListMapper
{
    #region MapListItemresponses

    private static Task<List<ListItemResponse>> MapListItemResponses(
        ItemListDbModel list,
        List<ItemListItemActionDbModel> itemActions,
        ItemsService itemsService,
        ItemPriceRefreshDbModel priceRefresh,
        List<ItemPriceDbModel> prices)
    {
        var result = new List<ListItemResponse>();
        foreach (var itemActionsGroupByItemId in itemActions.GroupBy(action => action.ItemId))
        {
            var itemId = itemActionsGroupByItemId.Key;
            var itemResult = itemsService.GetById(itemId);
            if (itemResult.IsError)
            {
                throw new Exception("ItemId is not valid");
            }

            var item = MapListItemResponse(
                list,
                itemResult.Value,
                itemActionsGroupByItemId,
                priceRefresh,
                prices
            );
            result.Add(item);
        }

        return Task.FromResult(result);
    }

    public static ListItemResponse MapListItemResponse(
        ItemListDbModel list,
        ItemModel item,
        IEnumerable<ItemListItemActionDbModel> itemActionsForItem,
        ItemPriceRefreshDbModel priceRefresh,
        IEnumerable<ItemPriceDbModel> pricesForItem)
    {
        var priceForItem = pricesForItem.First(price =>
            price.ItemPriceRefresh.Id == priceRefresh.Id && price.ItemId == item.Id);
        long? steamSellPrice;
        long? buff163SellPrice;

        if (CurrencyHelper.IsCurrencyValid(list.Currency) == false)
        {
            throw new UnknownCurrencyException(list.Currency);
        }

        if (list.Currency.Equals(CurrenciesConstants.USD))
        {
            steamSellPrice = priceForItem.SteamPriceCentsUsd;
            buff163SellPrice = priceForItem.Buff163PriceCentsUsd;
        }
        else if (list.Currency.Equals(CurrenciesConstants.EURO))
        {
            steamSellPrice = priceForItem.SteamPriceCentsUsd is null
                ? null
                : ExchangeRateHelper.ApplyExchangeRate(priceForItem.SteamPriceCentsUsd.Value,
                    priceRefresh.UsdToEurExchangeRate);
            buff163SellPrice = priceForItem.Buff163PriceCentsUsd is null
                ? null
                : ExchangeRateHelper.ApplyExchangeRate(priceForItem.Buff163PriceCentsUsd.Value,
                    priceRefresh.UsdToEurExchangeRate);
        }
        else
        {
            throw new NotImplementedException($"Currency \"{list.Currency}\" is not implemented");
        }

        var itemActionResponses = new List<ListItemActionResponse>();
        var buyPrices = new List<long>();
        long salesValue = 0;
        long profit = 0;
        var amountInvested = 0;
        var gotSales = false;
        foreach (var itemAction in itemActionsForItem.OrderBy(action => action.CreatedUtc))
        {
            if (itemAction.List.Id != list.Id)
            {
                throw new Exception(
                    $"Action is for the list with the id \"{itemAction.List.Id}\" but it should be for the list with the id \"{list.Id}\"");
            }

            if (itemAction.ItemId != item.Id)
            {
                throw new Exception(
                    $"Action itemId \"{itemAction.ItemId}\" dose not match the required itemId \"{item.Id}\"");
            }

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
            steamSellPrice,
            buff163SellPrice,
            salesValue,
            profit,
            itemActionResponses
        );
        return listItemResponse;
    }

    #endregion

    #region ListSnapshotResponse

    public static ErrorOr<ListSnapshotResponse> ListSnapshotResponseNew(
        ItemListSnapshotDbModel itemListSnapshot,
        List<ItemListItemActionDbModel> listActions,
        List<ItemPriceDbModel> prices)
    {
        var list = itemListSnapshot.List;

        long totalInvestedCapital = 0;
        long totalItemCount = 0;
        long salesValue = 0;
        long profit = 0;
        long steamValueUsdCent = 0;
        long buff163ValueUsdCent = 0;
        var actionsGroupedByItem = listActions.GroupBy(action => action.ItemId);
        foreach (var actionItemGroup in actionsGroupedByItem)
        {
            var buyPricesForItemId = new List<long>();
            var itemCountForItemId = 0;
            var actionsForSnapshot = actionItemGroup
                .Where(action => action.CreatedUtc <= itemListSnapshot.CreatedUtc)
                .OrderBy(action => action.CreatedUtc);
            foreach (var action in actionsForSnapshot)
            {
                if (action.List.Id != list.Id)
                {
                    return Error.Failure(
                        $"One item action (ActionId: {action.Id} | ListId {action.List.Id}) is not for the List from the list snapshot (SnapshotId: {itemListSnapshot.Id} | ListId: {itemListSnapshot.List.Id})");
                }

                if (action.Action.Equals("B"))
                {
                    if (itemCountForItemId == 0)
                    {
                        buyPricesForItemId.Clear();
                    }

                    buyPricesForItemId.AddRange(Enumerable.Repeat(action.UnitPrice, action.Amount));
                    itemCountForItemId += action.Amount;
                }
                else if (action.Action.Equals("S"))
                {
                    salesValue += action.UnitPrice * action.Amount;
                    profit += (long)Math.Round((action.UnitPrice - buyPricesForItemId.Average()) * action.Amount, 0);
                    itemCountForItemId -= action.Amount;
                }
            }


            if (buyPricesForItemId.Count > 0 && itemCountForItemId > 0)
            {
                totalInvestedCapital += (long)Math.Round(buyPricesForItemId.Average() * itemCountForItemId, 0);
            }

            totalItemCount += itemCountForItemId;

            var itemId = actionItemGroup.Key;
            var priceForItemId = prices.FirstOrDefault(price =>
                price.ItemPriceRefresh.Id == itemListSnapshot.ItemPriceRefresh.Id && price.ItemId == itemId);
            if (priceForItemId is not null)
            {
                if (priceForItemId.SteamPriceCentsUsd is not null)
                {
                    steamValueUsdCent += priceForItemId.SteamPriceCentsUsd.Value * itemCountForItemId;
                }

                if (priceForItemId.Buff163PriceCentsUsd is not null)
                {
                    buff163ValueUsdCent += priceForItemId.Buff163PriceCentsUsd.Value * itemCountForItemId;
                }
            }
        }

        long steamValue;
        long buff163Value;
        if (list.Currency.Equals(CurrenciesConstants.USD))
        {
            steamValue = steamValueUsdCent;
            buff163Value = buff163ValueUsdCent;
        }
        else if (list.Currency.Equals(CurrenciesConstants.EURO))
        {
            var usdToEurExchangeRate = itemListSnapshot.ItemPriceRefresh.UsdToEurExchangeRate;
            steamValue = ExchangeRateHelper.ApplyExchangeRate(steamValueUsdCent, usdToEurExchangeRate);
            buff163Value = ExchangeRateHelper.ApplyExchangeRate(buff163ValueUsdCent, usdToEurExchangeRate);
        }
        else
        {
            return Error.Failure(description: $"Currency \"{list.Currency}\" is not implemented");
        }

        return new ListSnapshotResponse(
            totalInvestedCapital,
            totalItemCount,
            salesValue,
            profit,
            steamValue,
            buff163Value,
            itemListSnapshot.CreatedUtc
        );
    }

    private static ErrorOr<List<ListSnapshotResponse>> MapListSnapshotResponses(
        IReadOnlyCollection<ItemListSnapshotDbModel> listSnapshots,
        List<ItemListItemActionDbModel> listActions,
        List<ItemPriceDbModel> prices)
    {
        var result = new List<ListSnapshotResponse>();
        if (listSnapshots.Count == 0)
        {
            return result;
        }

        if (listActions.Count == 0)
        {
            foreach (var snapshot in listSnapshots)
            {
                result.Add(new ListSnapshotResponse(0, 0, 0, 0, 0, 0, snapshot.CreatedUtc));
            }

            return result;
        }

        foreach (var snapshot in listSnapshots.OrderBy(value => value.CreatedUtc))
        {
            var listSnapshotResponse = ListSnapshotResponseNew(snapshot, listActions, prices);
            if (listSnapshotResponse.IsError)
            {
                return listSnapshotResponse.FirstError;
            }
            result.Add(listSnapshotResponse.Value);
        }

        return result;
    }

    #endregion


    public static async Task<ErrorOr<ListResponse>> MapToListResponse(
        ItemListDbModel itemListDbModel,
        List<ItemListSnapshotDbModel> itemListSnapshots,
        List<ItemListItemActionDbModel> itemListItemActions,
        ItemsService itemsService,
        ItemPriceRefreshDbModel priceRefresh,
        List<ItemPriceDbModel> prices)
    {
        var mapListItemResponsesTask = MapListItemResponses(
            itemListDbModel,
            itemListItemActions,
            itemsService,
            priceRefresh,
            prices
        );
        var listSnapshotResponses = MapListSnapshotResponses(itemListSnapshots, itemListItemActions, prices);
        if (listSnapshotResponses.IsError)
        {
            return listSnapshotResponses.FirstError;
        }
        await mapListItemResponsesTask;

        var listResponse = new ListResponse(
            itemListDbModel.Name,
            itemListDbModel.Description,
            itemListDbModel.Url,
            itemListDbModel.Currency,
            itemListDbModel.Public,
            itemListDbModel.UserId,
            mapListItemResponsesTask.Result,
            listSnapshotResponses.Value);

        return listResponse;
    }
}