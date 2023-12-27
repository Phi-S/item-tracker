using application.Cache;
using ErrorOr;
using infrastructure.Database.Models;
using infrastructure.Database.Repos;
using infrastructure.ExchangeRates;
using infrastructure.Items;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using shared.Currencies;
using shared.Models.ListResponse;

namespace application.Queries;

public record GetListQuery(string? UserId, string ListUrl) : IRequest<ErrorOr<ListResponse>>;

public class GetListHandler : IRequestHandler<GetListQuery, ErrorOr<ListResponse>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;
    private readonly ItemsService _itemsService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public GetListHandler(
        UnitOfWork unitOfWork,
        ListResponseCacheService listResponseCacheService,
        ItemsService itemsService,
        IServiceScopeFactory serviceScopeFactory)
    {
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
        _itemsService = itemsService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<ErrorOr<ListResponse>> Handle(GetListQuery request, CancellationToken cancellationToken)
    {
        var list = await _unitOfWork.ItemListRepo.GetListByUrl(request.ListUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.Public == false && list.Value.UserId.Equals(request.UserId) == false)
        {
            return Error.Unauthorized(description: "You dont have access to this list");
        }

        var cachedListResponse = _listResponseCacheService.GetListResponse(request.ListUrl);
        if (cachedListResponse.IsError == false)
        {
            return cachedListResponse.Value;
        }

        var listResponse = await GetListResponse(request.ListUrl);
        if (listResponse.IsError)
        {
            return listResponse.FirstError;
        }

        _listResponseCacheService.UpdateCache(listResponse.Value);
        return listResponse;
    }

    private async Task<ErrorOr<ListResponse>> GetListResponse(string listUrl, int snapshotsForLastDays = 30)
    {
        var snapshotStartDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(snapshotsForLastDays));

        #region GetList

        var listResult = await _unitOfWork.ItemListRepo.GetListByUrl(listUrl);
        if (listResult.IsError)
        {
            return listResult.FirstError;
        }

        var list = listResult.Value;

        #endregion

        var priceRefreshes = await _unitOfWork.ItemPriceRepo.GetSince(snapshotStartDate);
        if (priceRefreshes.Count == 0)
        {
            var latestPriceRefresh = await _unitOfWork.ItemPriceRepo.GetLatest();
            if (latestPriceRefresh.IsError)
            {
                if (latestPriceRefresh.FirstError != Error.NotFound())
                {
                    return latestPriceRefresh.FirstError;
                }
            }
            else
            {
                priceRefreshes.Add(latestPriceRefresh.Value);
            }
        }

        #region CreateSnapshotDays

        var snapshotsDays = new List<DateOnly>();
        for (var i = 0; i < snapshotsForLastDays; i++)
        {
            var date = DateOnly.FromDateTime(
                snapshotStartDate.Add(TimeSpan.FromDays(i))
                    .Date
            );
            snapshotsDays.Add(date);
        }

        #endregion

        #region Process

        var processItemsTasks =
            new List<Task<ErrorOr<(ListItemResponse listItemResponse, Dictionary<DateOnly, ItemSnapshotForDay>
                snapshotsResult)>>>();
        var actions = await _unitOfWork.ItemListRepo.GetAllItemActionsForList(list.Id);
        var itemIdsWithActionsGrouping = actions.GroupBy(action => action.ItemId).ToList();
        foreach (var itemIdsWithActions in itemIdsWithActionsGrouping)
        {
            processItemsTasks.Add(
                GetListItemResponseWithItemSnapshots(snapshotsDays, list, itemIdsWithActions, priceRefreshes)
            );
        }

        await Task.WhenAll(processItemsTasks);

        var listItemResponses = new List<ListItemResponse>();
        var itemSnapshots = new Dictionary<DateOnly, List<ItemSnapshotForDay>>();

        long listInvestedCapital = 0;
        var listItemCount = 0;
        long? listSteamPrice = null;
        long? listBuff163Price = null;
        long? listSteamPerformanceValue = null;
        long? listBuff163PerformanceValue = null;
        foreach (var processItemsTask in processItemsTasks)
        {
            var result = processItemsTask.Result;
            if (result.IsError)
            {
                return result.FirstError;
            }

            var (listItemResponse, snapshotsResult) = result.Value;
            listItemResponses.Add(listItemResponse);
            foreach (var snapshotForDay in snapshotsResult)
            {
                if (itemSnapshots.ContainsKey(snapshotForDay.Key) == false)
                {
                    itemSnapshots.Add(snapshotForDay.Key, [snapshotForDay.Value]);
                }
                else
                {
                    itemSnapshots[snapshotForDay.Key].Add(snapshotForDay.Value);
                }
            }

            listInvestedCapital += listItemResponse.InvestedCapital;
            listItemCount += listItemResponse.ItemCount;
            if (listItemResponse.SteamSellPriceForOne is not null)
            {
                listSteamPrice ??= 0;
                listSteamPrice += listItemResponse.SteamSellPriceForOne.Value * listItemResponse.ItemCount;
            }

            if (listItemResponse.Buff163SellPriceForOne is not null)
            {
                listBuff163Price ??= 0;
                listBuff163Price += listItemResponse.Buff163SellPriceForOne.Value * listItemResponse.ItemCount;
            }

            if (listItemResponse.SteamPerformanceValue is not null)
            {
                listSteamPerformanceValue ??= 0;
                listSteamPerformanceValue += listItemResponse.SteamPerformanceValue;
            }

            if (listItemResponse.Buff163PerformanceValue is not null)
            {
                listBuff163PerformanceValue ??= 0;
                listBuff163PerformanceValue += listItemResponse.Buff163PerformanceValue;
            }
        }

        var totalSteamPerformancePercent = GetPerformancePercent(listSteamPrice, listInvestedCapital);
        var totalBuff163PerformancePercent = GetPerformancePercent(listBuff163Price, listInvestedCapital);

        var snapshots = itemSnapshots.Select(pair => GetListSnapshot(pair.Key, pair.Value)).ToList();

        #endregion

        var listResponse = new ListResponse(
            list.Name,
            list.Description,
            list.Url,
            list.Currency,
            list.Public,
            list.UserId,
            listItemCount,
            listInvestedCapital,
            listSteamPrice,
            listBuff163Price,
            totalSteamPerformancePercent,
            totalBuff163PerformancePercent,
            listSteamPerformanceValue,
            listBuff163PerformanceValue,
            listItemResponses,
            snapshots
        );

        return listResponse;
    }

    private record ItemSnapshotForDay(
        long TotalInvestedCapital,
        long TotalItemCount,
        long SalesValue,
        long Profit,
        long? SteamValueForOne,
        long? Buff163ValueForOne
    );

    private async
        Task<ErrorOr<(ListItemResponse listItemResponse, Dictionary<DateOnly, ItemSnapshotForDay> snapshotsResult)>>
        GetListItemResponseWithItemSnapshots(
            IEnumerable<DateOnly> snapshotDays,
            ItemListDbModel list,
            IGrouping<long, ItemListItemActionDbModel> itemWithActions,
            IReadOnlyCollection<ItemPriceRefreshDbModel> priceRefreshes)
    {
        var itemId = itemWithActions.Key;
        var itemActionResponses = new List<ListItemActionResponse>();

        var buyPrices = new List<long>();
        var itemCount = 0;
        long salesValue = 0;
        long salesProfit = 0;
        var gotSales = false;

        var listCreationDay = DateOnly.FromDateTime(list.CreatedUtc);
        snapshotDays = snapshotDays.Order().ToList();
        using var snapshotDaysEnumerator = snapshotDays.GetEnumerator();

        var snapshotsResult = new Dictionary<DateOnly, ItemSnapshotForDay>();
        var noMoreSnapshotDaysLeft = false;
        // If snapshots are created before the list was created,
        // skip to snapshot when the list was created
        while (true)
        {
            snapshotsResult.Add(snapshotDaysEnumerator.Current, new ItemSnapshotForDay(0, 0, 0, 0, null, null));
            if (snapshotDaysEnumerator.MoveNext() == false)
            {
                noMoreSnapshotDaysLeft = true;
                break;
            }

            if (snapshotDaysEnumerator.Current.Equals(listCreationDay))
            {
                break;
            }
        }

        var actions = itemWithActions.OrderBy(action => action.CreatedUtc).ToList();
        foreach (var action in actions)
        {
            // TODO: check if all actions belong to the given list. Repo need include so action.list is populated. Is it worth?

            #region CreateListItemActionResponse

            var actionResponse = new ListItemActionResponse(
                action.Id,
                action.Action,
                action.Amount,
                action.UnitPrice,
                action.CreatedUtc
            );
            itemActionResponses.Add(actionResponse);

            #endregion

            #region CreateSnapshot

            // if the next action is for the snapshot of the next day,
            // crete snapshot for all actions until the current action
            if (noMoreSnapshotDaysLeft == false)
            {
                var actionCreationDayUtc = DateOnly.FromDateTime(action.CreatedUtc);
                var currentSnapshotDay = snapshotDaysEnumerator.Current;
                while (currentSnapshotDay < actionCreationDayUtc)
                {
                    var closestPriceRefresh = priceRefreshes
                        .Where(priceRefresh =>
                            DateOnly.FromDateTime(priceRefresh.CreatedUtc) <= currentSnapshotDay)
                        .MaxBy(priceRefresh => priceRefresh.CreatedUtc);

                    var snap = await GetItemSnapshotForDay(
                        itemCount,
                        buyPrices,
                        closestPriceRefresh,
                        list.Currency,
                        itemId,
                        salesValue,
                        salesProfit
                    );
                    if (snap.IsError)
                    {
                        return snap.FirstError;
                    }

                    snapshotsResult[currentSnapshotDay] = snap.Value;
                    if (snapshotDaysEnumerator.MoveNext() == false)
                    {
                        noMoreSnapshotDaysLeft = true;
                        break;
                    }

                    currentSnapshotDay = snapshotDaysEnumerator.Current;
                }
            }

            #endregion

            #region ProcessAction

            if (action.Action.Equals("B"))
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

                buyPrices.AddRange(Enumerable.Repeat(action.UnitPrice, action.Amount));
                itemCount += action.Amount;
            }
            else if (action.Action.Equals("S"))
            {
                gotSales = true;
                salesValue += action.UnitPrice * action.Amount;
                salesProfit += (long)Math.Round(
                    (action.UnitPrice - buyPrices.Average()) * action.Amount,
                    0);
                itemCount -= action.Amount;
            }

            #endregion
        }

        #region CreateRemainingSnapshots

        // snapshots are left
        // create snapshots for the rest
        if (noMoreSnapshotDaysLeft == false)
        {
            while (true)
            {
                var currentSnapshotDay = snapshotDaysEnumerator.Current;
                var closestPriceRefresh = priceRefreshes
                    .Where(priceRefresh =>
                        DateOnly.FromDateTime(priceRefresh.CreatedUtc) <= currentSnapshotDay)
                    .MaxBy(priceRefresh => priceRefresh.CreatedUtc);

                var snap = await GetItemSnapshotForDay(
                    itemCount,
                    buyPrices,
                    closestPriceRefresh,
                    list.Currency,
                    itemId,
                    salesValue,
                    salesProfit
                );
                if (snap.IsError)
                {
                    return snap.FirstError;
                }

                snapshotsResult[currentSnapshotDay] = snap.Value;
                if (snapshotDaysEnumerator.MoveNext() == false)
                {
                    break;
                }
            }
        }

        var latestPriceRefresh = priceRefreshes.MaxBy(priceRefresh => priceRefresh.CreatedUtc);
        var mostCurrentSnap = await GetItemSnapshotForDay(
            itemCount,
            buyPrices,
            latestPriceRefresh,
            list.Currency,
            itemId,
            salesValue,
            salesProfit
        );
        if (mostCurrentSnap.IsError)
        {
            return mostCurrentSnap.FirstError;
        }

        snapshotsResult[DateOnly.FromDateTime(DateTime.UtcNow.Date)] = mostCurrentSnap.Value;

        #endregion

        #region CreateListItemResponse

        var averageBuyPrice = buyPrices.Count == 0 ? 0 : buyPrices.Average();
        var capitalInvested = gotSales ? (long)Math.Round(averageBuyPrice * itemCount, 0) : buyPrices.Sum();

        long? steamPrice = null;
        long? buff163Price = null;

        if (latestPriceRefresh is not null)
        {
            var latestPricesResult = await GetPrice(list.Currency, itemId, latestPriceRefresh);
            if (latestPricesResult.IsError)
            {
                return latestPricesResult.FirstError;
            }

            var latestPrice = latestPricesResult.Value;
            steamPrice = latestPrice.steamPrice;
            buff163Price = latestPrice.buff163Price;
        }

        var steamPerformancePercent = GetPerformancePercent(steamPrice, averageBuyPrice);
        var buff163PerformancePercent = GetPerformancePercent(buff163Price, averageBuyPrice);

        var steamPerformanceValue = GetPerformanceValue(steamPrice, averageBuyPrice, itemCount);
        var buff163PerformanceValue = GetPerformanceValue(buff163Price, averageBuyPrice, itemCount);

        var itemResult = _itemsService.GetById(itemId);
        if (itemResult.IsError)
        {
            throw new Exception("ItemId is not valid");
        }

        var item = itemResult.Value;
        var listItemResponse = new ListItemResponse(
            item.Id,
            item.Name,
            item.Image,
            itemCount,
            capitalInvested,
            (long)Math.Round(averageBuyPrice, 0),
            steamPrice,
            buff163Price,
            steamPerformancePercent,
            buff163PerformancePercent,
            steamPerformanceValue,
            buff163PerformanceValue,
            salesValue,
            salesProfit,
            itemActionResponses
        );

        #endregion

        return (listItemResponse, snapshotsResult);
    }

    private async Task<ErrorOr<ItemSnapshotForDay>> GetItemSnapshotForDay(
        int itemCount,
        List<long> buyPrices,
        ItemPriceRefreshDbModel? closestPriceRefresh,
        string currency,
        long itemId,
        long salesValue,
        long salesProfit)
    {
        long investedCapitalForItem = 0;
        if (buyPrices.Count > 0 && itemCount > 0)
        {
            investedCapitalForItem = (long)Math.Round(buyPrices.Average() * itemCount, 0);
        }

        if (closestPriceRefresh is null)
        {
            return new ItemSnapshotForDay(
                investedCapitalForItem,
                itemCount,
                salesValue,
                salesProfit,
                null,
                null
            );
        }

        var getPriceResult = await GetPrice(currency, itemId, closestPriceRefresh);
        if (getPriceResult.IsError)
        {
            return getPriceResult.FirstError;
        }

        var (steamPrice, buff163Price) = getPriceResult.Value;
        var snap = new ItemSnapshotForDay(
            investedCapitalForItem,
            itemCount,
            salesValue,
            salesProfit,
            steamPrice,
            buff163Price
        );
        return snap;
    }

    private static ListSnapshotResponse GetListSnapshot(DateOnly snapshotDay, List<ItemSnapshotForDay> snapshotsForDay)
    {
        if (snapshotsForDay.Count == 0)
        {
            return new ListSnapshotResponse(
                0,
                0,
                0,
                0,
                null,
                null,
                snapshotDay
            );
        }

        long totalInvestedCapital = 0;
        long totalItemCount = 0;
        long salesValue = 0;
        long profit = 0;
        long? steamValue = 0;
        long? buff163Value = 0;
        foreach (var snapshotForItem in snapshotsForDay)
        {
            totalInvestedCapital += snapshotForItem.TotalInvestedCapital;
            totalItemCount += snapshotForItem.TotalItemCount;
            salesValue += snapshotForItem.SalesValue;
            profit += snapshotForItem.Profit;
            if (snapshotForItem.SteamValueForOne is not null)
            {
                steamValue += snapshotForItem.SteamValueForOne * snapshotForItem.TotalItemCount;
            }

            if (snapshotForItem.Buff163ValueForOne is not null)
            {
                buff163Value += snapshotForItem.Buff163ValueForOne * snapshotForItem.TotalItemCount;
            }
        }

        return new ListSnapshotResponse(
            totalInvestedCapital,
            totalItemCount,
            salesValue,
            profit,
            steamValue,
            buff163Value,
            snapshotDay
        );
    }

    private static double? GetPerformancePercent(long? current, double old)
    {
        if (current is null || old == 0)
        {
            return null;
        }

        var performance = Math.Round((double)current / old * 100 - 100, 2);
        return double.IsInfinity(performance) ? null : performance;
    }

    private static long? GetPerformanceValue(long? current, double old, int itemCount)
    {
        return (long?)((current - old) * itemCount);
    }

    private async Task<ErrorOr<(long? steamPrice, long? buff163Price)>> GetPrice(
        string currency,
        long itemId,
        ItemPriceRefreshDbModel priceRefresh
    )
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();

        var priceForItemIdResult =
            await unitOfWork.ItemPriceRepo.GetPriceForItem(itemId, priceRefresh);
        if (priceForItemIdResult.IsError)
        {
            if (priceForItemIdResult.FirstError.Type == ErrorType.NotFound)
            {
                return (null, null);
            }

            return priceForItemIdResult.FirstError;
        }

        var priceForItemId = priceForItemIdResult.Value;

        long? steamValue = null;
        if (priceForItemId.SteamPriceCentsUsd is not null)
        {
            if (currency.Equals(CurrenciesConstants.USD))
            {
                steamValue = priceForItemId.SteamPriceCentsUsd.Value;
            }
            else if (currency.Equals(CurrenciesConstants.EURO))
            {
                steamValue = ExchangeRateHelper.ApplyExchangeRate(
                    priceForItemId.SteamPriceCentsUsd.Value,
                    priceRefresh.UsdToEurExchangeRate);
            }
            else
            {
                return Error.Failure(description: $"Currency \"{currency}\" is not implemented");
            }
        }

        long? buff163Value = null;
        if (priceForItemId.Buff163PriceCentsUsd is not null)
        {
            if (currency.Equals(CurrenciesConstants.USD))
            {
                buff163Value = priceForItemId.Buff163PriceCentsUsd.Value;
            }
            else if (currency.Equals(CurrenciesConstants.EURO))
            {
                buff163Value = ExchangeRateHelper.ApplyExchangeRate(
                    priceForItemId.Buff163PriceCentsUsd.Value,
                    priceRefresh.UsdToEurExchangeRate);
            }
            else
            {
                return Error.Failure(description: $"Currency \"{currency}\" is not implemented");
            }
        }

        return (steamValue, buff163Value);
    }
}