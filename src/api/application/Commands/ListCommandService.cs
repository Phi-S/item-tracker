using application.Mapper;
using ErrorOr;
using infrastructure.Database.Models;
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
    private const int BuySellLimit = 10000;
    
    public ListCommandService(ItemsService itemsService, UnitOfWork unitOfWork)
    {
        _itemsService = itemsService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<List<ListResponse>>> GetAllForUser(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var lists = await _unitOfWork.ItemListRepo.GetListInfosForUserId(userId);
        var mapToListResponseTasks = new List<Task<ListResponse>>();
        foreach (var (list, values, items) in lists)
        {
            mapToListResponseTasks.Add(ItemListMapper.MapToListResponse(list, values, items, _itemsService));
        }

        await Task.WhenAll(mapToListResponseTasks);
        return mapToListResponseTasks.Select(task => task.Result).ToList();
    }

    public async Task<ErrorOr<ListResponse>> New(string? userId, NewListModel newListModel)
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

        // TODO: itemlistdbmodel as parameter and check if url exists before adding?
        var list = await _unitOfWork.ItemListRepo.CreateNewList(
            userId,
            newListModel.ListName,
            newListModel.ListDescription,
            newListModel.Currency,
            newListModel.Public
        );
  
        var listValue = await _unitOfWork.ItemListSnapshotRepo.CalculateWithLatestPrices(list);
        var listResponse = await ItemListMapper.MapToListResponse(
            list,
            [listValue],
            [],
            _itemsService);

        await _unitOfWork.Save();
        return listResponse;
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

        var listInfos = _unitOfWork.ItemListRepo.GetListInfos(list.Value.Id);
        var listResponse = await ItemListMapper.MapToListResponse(
            listInfos.list,
            listInfos.listValues,
            listInfos.items,
            _itemsService);
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
        await _unitOfWork.ItemListSnapshotRepo.CalculateWithLatestPrices(list.Value);
        await _unitOfWork.Save();
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

        var currentItemCount = await _unitOfWork.ItemListRepo.GetCurrentItemCount(list.Value, itemId);
        if (amount > currentItemCount)
        {
            return Error.Conflict(description:
                $"Cant sell {amount} items if the list \"{listUrl}\" only contains {currentItemCount} items with the id \"{itemId}\"");
        }

        await _unitOfWork.ItemListRepo.AddItemAction("S", list.Value, itemId, unitPrice, amount);
        await _unitOfWork.Save();
        await _unitOfWork.ItemListSnapshotRepo.CalculateWithLatestPrices(list.Value);
        await _unitOfWork.Save();
        return Result.Created;
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
        return Result.Updated;
    }
}