using ErrorOr;

namespace application.Commands.List;

public partial class ListCommandService
{
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

        var list = await _unitOfWork.ItemListRepo.GetListByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(
                description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
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
        var latestPriceRefresh = await _unitOfWork.ItemPriceRepo.GetLatest();
        if (latestPriceRefresh.IsError)
        {
            return latestPriceRefresh.FirstError;
        }

        await _unitOfWork.ItemListRepo.NewSnapshot(list.Value, latestPriceRefresh.Value);
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

        var list = await _unitOfWork.ItemListRepo.GetListByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(
                description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        if (amount <= 0)
        {
            return Error.Failure(description: $"Cant sell {amount} items");
        }

        if (amount >= BuySellLimit)
        {
            return Error.Failure(description: "Cant sell more then 5000 items at once");
        }

        var currentItemCount = await _unitOfWork.ItemListRepo.GetListItemCount(list.Value.Id, itemId);
        if (amount > currentItemCount)
        {
            return Error.Conflict(description:
                $"Cant sell {amount} items if the list \"{listUrl}\" only contains {currentItemCount} items with the id \"{itemId}\"");
        }

        await _unitOfWork.ItemListRepo.AddItemAction("S", list.Value, itemId, unitPrice, amount);
        var latestPriceRefresh = await _unitOfWork.ItemPriceRepo.GetLatest();
        if (latestPriceRefresh.IsError)
        {
            return latestPriceRefresh.FirstError;
        }

        await _unitOfWork.ItemListRepo.NewSnapshot(list.Value, latestPriceRefresh.Value);
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
        var latestPriceRefresh = await _unitOfWork.ItemPriceRepo.GetLatest();
        if (latestPriceRefresh.IsError)
        {
            return latestPriceRefresh.FirstError;
        }

        await _unitOfWork.ItemListRepo.NewSnapshot(action.List, latestPriceRefresh.Value);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(action.List.Url);
        return Result.Deleted;
    }
}