using application.Cache;
using application.Mapper;
using ErrorOr;
using infrastructure.Database.Repos;
using infrastructure.Items;
using shared.Currencies;
using shared.Models;
using shared.Models.ListResponse;

namespace application.Commands;

public class ListCommandService
{
    private readonly ItemsService _itemsService;
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;
    private const int BuySellLimit = 5000;

    public ListCommandService(ItemsService itemsService, UnitOfWork unitOfWork,
        ListResponseCacheService listResponseCacheService)
    {
        _itemsService = itemsService;
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
    }

    public async Task<ErrorOr<List<ListResponse>>> GetAllForUser(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var cachedListResponses = new List<ListResponse>();
        var lists = await _unitOfWork.ItemListRepo.GetAllListsForUser(userId);
        var mapToListResponseTasks = new List<Task<ListResponse>>();
        foreach (var list in lists)
        {
            var cacheResponse = _listResponseCacheService.GetListResponse(list.Url);
            if (cacheResponse.IsError)
            {
                var listInfo = await _unitOfWork.ItemListRepo.GetListInfos(list.Id);
                mapToListResponseTasks.Add(ItemListMapper.MapToListResponse(list, listInfo.Snapshots,
                    listInfo.ItemActions,
                    _itemsService, listInfo.LastPriceRefresh, listInfo.PricesForItemsInList));
            }
            else
            {
                cachedListResponses.Add(cacheResponse.Value);
            }
        }

        await Task.WhenAll(mapToListResponseTasks);
        var listResponses = mapToListResponseTasks.Select(task => task.Result).ToList();
        foreach (var listResponse in listResponses)
        {
            _listResponseCacheService.UpdateCache(listResponse);
        }

        listResponses.AddRange(cachedListResponses);
        return listResponses;
    }

    public async Task<ErrorOr<string>> New(string? userId, NewListModel newListModel)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var isCurrencyValid = CurrencyHelper.IsCurrencyValid(newListModel.Currency);
        if (isCurrencyValid == false)
        {
            return Error.Failure(description: $"Currency \"{newListModel.Currency}\" is not a valid currency");
        }

        var existingListWithName = await _unitOfWork.ItemListRepo.ExistsWithNameForUser(userId, newListModel.ListName);
        if (existingListWithName)
        {
            return Error.Conflict(description: $"List with the name \"{newListModel.ListName}\" already exist");
        }

        var url = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        // Replace URL unfriendly characters
        url = url
            .Replace("=", "")
            .Replace("/", "_")
            .Replace("+", "-");

        var list = await _unitOfWork.ItemListRepo.CreateNewList(
            userId,
            url,
            newListModel.ListName,
            newListModel.ListDescription,
            newListModel.Currency,
            newListModel.Public
        );

        var snapshot = await _unitOfWork.ItemListSnapshotRepo.CalculateWithLatestPrices(list);
        if (snapshot.IsError)
        {
            return snapshot.FirstError;
        }

        await _unitOfWork.Save();
        return url;
    }

    public async Task<ErrorOr<ListResponse>> GetList(string? userId, string listUrl)
    {
        var list = await _unitOfWork.ItemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.Public == false && list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: "You dont have access to this list");
        }

        var cachedListResponse = _listResponseCacheService.GetListResponse(listUrl);
        if (cachedListResponse.IsError == false)
        {
            return cachedListResponse.Value;
        }

        var listInfos = await _unitOfWork.ItemListRepo.GetListInfos(list.Value.Id);
        var listResponse = await ItemListMapper.MapToListResponse(
            listInfos.List,
            listInfos.Snapshots,
            listInfos.ItemActions,
            _itemsService,
            listInfos.LastPriceRefresh,
            listInfos.PricesForItemsInList
        );
        _listResponseCacheService.UpdateCache(listResponse);
        return listResponse;
    }

    public async Task<ErrorOr<Deleted>> DeleteList(string? userId, string listUrl)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _unitOfWork.ItemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: "You dont have access to this list");
        }

        await _unitOfWork.ItemListRepo.DeleteList(list.Value.Id);
        _listResponseCacheService.DeleteCache(listUrl);
        await _unitOfWork.Save();
        return Result.Deleted;
    }

    public async Task<ErrorOr<Created>> BuyItem(
        string? userId,
        string listUrl,
        long itemId,
        long unitPrice,
        int amount)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var item = _itemsService.GetById(itemId);
        if (item.IsError)
        {
            return item.FirstError;
        }

        var list = await _unitOfWork.ItemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        if (amount <= 0)
        {
            return Error.Failure(description: $"Cant buy {amount} items");
        }

        if (amount >= BuySellLimit)
        {
            return Error.Failure(description: "Cant buy more then 5000 items at once");
        }

        await _unitOfWork.ItemListRepo.AddItemAction("B", list.Value, itemId, unitPrice, amount);
        await _unitOfWork.Save();
        var snapshot = await _unitOfWork.ItemListSnapshotRepo.CalculateWithLatestPrices(list.Value);
        if (snapshot.IsError)
        {
            return snapshot.FirstError;
        }

        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(listUrl);
        return Result.Created;
    }

    public async Task<ErrorOr<Created>> SellItem(
        string? userId,
        string listUrl,
        long itemId,
        long unitPrice,
        int amount)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var item = _itemsService.GetById(itemId);
        if (item.IsError)
        {
            return item.FirstError;
        }

        var list = await _unitOfWork.ItemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        if (amount <= 0)
        {
            return Error.Failure(description: $"Cant sell {amount} items");
        }

        if (amount >= BuySellLimit)
        {
            return Error.Failure(description: "Cant sell more then 5000 items at once");
        }

        var currentItemCount = await _unitOfWork.ItemListRepo.GetItemsInList(list.Value.Id, itemId);
        if (amount > currentItemCount)
        {
            return Error.Conflict(description:
                $"Cant sell {amount} items if the list \"{listUrl}\" only contains {currentItemCount} items with the id \"{itemId}\"");
        }

        await _unitOfWork.ItemListRepo.AddItemAction("S", list.Value, itemId, unitPrice, amount);
        await _unitOfWork.Save();
        var snapshot = await _unitOfWork.ItemListSnapshotRepo.CalculateWithLatestPrices(list.Value);
        if (snapshot.IsError)
        {
            return snapshot.FirstError;
        }

        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(listUrl);
        return Result.Created;
    }

    public async Task<ErrorOr<Deleted>> DeleteItemAction(string? userId, long itemActionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var action = await _unitOfWork.ItemListRepo.GetItemActionById(itemActionId);

        if (action.List.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(
                description: $"The list \"{action.List.Url}\" dose not belong to the user \"{userId}\"");
        }

        await _unitOfWork.ItemListRepo.DeleteItemAction(action.List, itemActionId);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(action.List.Url);
        return Result.Deleted;
    }

    public async Task<ErrorOr<Updated>> UpdateListName(string? userId, string listUrl, string newName)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _unitOfWork.ItemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _unitOfWork.ItemListRepo.UpdateName(list.Value.Id, newName);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(listUrl);
        return Result.Updated;
    }

    public async Task<ErrorOr<Updated>> UpdateListDescription(string? userId, string listUrl, string newDescription)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _unitOfWork.ItemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _unitOfWork.ItemListRepo.UpdateDescription(list.Value.Id, newDescription);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(listUrl);
        return Result.Updated;
    }

    public async Task<ErrorOr<Updated>> UpdateListPublic(string? userId, string listUrl, bool newPublic)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _unitOfWork.ItemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _unitOfWork.ItemListRepo.UpdatePublic(list.Value.Id, newPublic);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(listUrl);
        return Result.Updated;
    }
}