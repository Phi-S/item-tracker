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

    private static Task<ListResponse> MapListItemResponses(
        ItemListDbModel list,
        List<ItemListItemActionDbModel> itemActions,
        ItemsService itemsService,
        ItemPriceRefreshDbModel priceRefresh,
        List<ItemPriceDbModel> prices)
    {
        long totalInvestedCapital = 0;
        var totalItemCount = 0;
        long? totalSteamPrice = null;
        long? totalBuff163Price = null;
        long? totalSteamPerformanceValue = null;
        long? totalBuff163PerformanceValue = null;

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
            totalInvestedCapital += item.InvestedCapital;
            totalItemCount += item.ItemCount;
            if (item.SteamSellPriceForOne is not null)
            {
                totalSteamPrice ??= 0;
                totalSteamPrice += item.SteamSellPriceForOne.Value * item.ItemCount;
            }

            if (item.Buff163SellPriceForOne is not null)
            {
                totalBuff163Price ??= 0;
                totalBuff163Price += item.Buff163SellPriceForOne.Value * item.ItemCount;
            }

            totalSteamPerformanceValue += item.SteamPerformanceValue;
            totalBuff163PerformanceValue += item.Buff163PerformanceValue;
            result.Add(item);
        }

        var totalSteamPerformancePercent = GetPerformancePercent(totalSteamPrice, totalInvestedCapital);
        var totalBuff163PerformancePercent = GetPerformancePercent(totalBuff163Price, totalInvestedCapital);

        var listResponse = new ListResponse(
            list.Name,
            list.Description,
            list.Url,
            list.Currency,
            list.Public,
            list.UserId,
            totalItemCount,
            totalInvestedCapital,
            totalSteamPrice,
            totalBuff163Price,
            totalSteamPerformancePercent,
            totalBuff163PerformancePercent,
            totalSteamPerformanceValue,
            totalBuff163PerformanceValue,
            result,
            []
        );
        return Task.FromResult(listResponse);
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
        var itemCount = 0;
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
                if (itemCount == 0)
                {
                    buyPrices.Clear();
                }

                buyPrices.AddRange(Enumerable.Repeat(actionResponse.Price, actionResponse.Amount));
                itemCount += actionResponse.Amount;
            }
            else if (actionResponse.Action.Equals("S"))
            {
                gotSales = true;
                salesValue += actionResponse.Amount * actionResponse.Price;
                var tempAverageBuyPrice = (long)Math.Round(buyPrices.Average(), 0);
                profit += (actionResponse.Price - tempAverageBuyPrice) * actionResponse.Amount;
                itemCount -= actionResponse.Amount;
            }
        }

        var averageBuyPrice = buyPrices.Count == 0 ? 0 : (long)Math.Round(buyPrices.Average(), 0);
        var capitalInvested = gotSales ? averageBuyPrice * itemCount : buyPrices.Sum();

        var steamPerformancePercent = GetPerformancePercent(steamSellPrice, averageBuyPrice);
        var buff163PerformancePercent = GetPerformancePercent(buff163SellPrice, averageBuyPrice);

        var steamPerformanceValue = GetPerformanceValue(steamSellPrice, averageBuyPrice, itemCount);
        var buff163PerformanceValue = GetPerformanceValue(buff163SellPrice, averageBuyPrice, itemCount);

        var listItemResponse = new ListItemResponse(
            item.Id,
            item.Name,
            item.Image,
            itemCount,
            capitalInvested,
            averageBuyPrice,
            steamSellPrice,
            buff163SellPrice,
            steamPerformancePercent,
            buff163PerformancePercent,
            steamPerformanceValue,
            buff163PerformanceValue,
            salesValue,
            profit,
            itemActionResponses
        );
        return listItemResponse;
    }

    private static double? GetPerformancePercent(long? currentPrice, long buyPrice)
    {
        if (currentPrice is null)
        {
            return null;
        }
        var performance = Math.Round((double)(currentPrice ?? 0) / buyPrice * 100 - 100, 2);
        return double.IsInfinity(performance) ? null : performance;
    }

    private static long? GetPerformanceValue(long? currentPrice, long buyPrice, int itemCount)
    {
        return (currentPrice - buyPrice) * itemCount;
    }

    #endregion

    #region ListSnapshotResponse

    public static ErrorOr<ListSnapshotResponse> ListSnapshotResponse(
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
            var priceForItemId = prices.FirstOrDefault(price => price.ItemId == itemId);
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
            var listSnapshotResponse = ListSnapshotResponse(snapshot, listActions, prices);
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

        var listResponse = await mapListItemResponsesTask;
        listResponse.Snapshots.AddRange(listSnapshotResponses.Value);
        return listResponse;
    }
}