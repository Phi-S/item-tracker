using ErrorOr;
using infrastructure.Currencies;
using infrastructure.Database.Models;
using infrastructure.Database.Repos;
using infrastructure.Items;
using infrastructure.Mapper;
using shared.Models;
using shared.Models.ListResponse;
using Error = ErrorOr.Error;

namespace application.Commands;

public class ListCommandService
{
    private const int BuySellLimit = 10000;
    private readonly ItemListRepo _itemListRepo;
    private readonly ItemListSnapshotRepo _itemListSnapshotRepo;
    private readonly ItemsService _itemsService;

    public ListCommandService(
        ItemListRepo itemListRepo,
        ItemListSnapshotRepo itemListSnapshotRepo,
        ItemsService itemsService)
    {
        _itemListRepo = itemListRepo;
        _itemListSnapshotRepo = itemListSnapshotRepo;
        _itemsService = itemsService;
    }

    public async Task<ErrorOr<List<ListResponse>>> GetAllForUser(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var lists = await _itemListRepo.GetListInfosForUserId(userId);
        var result = new List<ListResponse>();
        foreach (var (list, values, items) in lists)
        {
            Console.WriteLine(list.Name);
            var listResponse = ItemListMapper.MapToListResponse(list, values, items, _itemsService);
            if (listResponse.IsError)
            {
                return listResponse.FirstError;
            }

            result.Add(listResponse.Value);
        }

        return result;
    }

    public async Task<ErrorOr<ListResponse>> New(string? userId, NewListModel newListModel)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var isCurrencyValid = CurrenciesHelper.IsCurrencyValid(newListModel.Currency);
        if (isCurrencyValid == false)
        {
            return Error.Failure(description: $"Currency \"{newListModel.Currency}\" is not a valid currency");
        }

        var existingListWithName = await _itemListRepo.ExistsWithNameForUser(userId, newListModel.ListName);
        if (existingListWithName)
        {
            return Error.Conflict(description: $"List with the name \"{newListModel.ListName}\" already exist");
        }

        // TODO: itemlistdbmodel as parameter and check if url exists before adding?
        var list = await _itemListRepo.CreateNewList(
            userId,
            newListModel.ListName,
            newListModel.ListDescription,
            newListModel.Currency,
            newListModel.Public
        );

        var listValue = await _itemListSnapshotRepo.CalculateWithLatestPrices(list);
        var listResponse = ItemListMapper.MapToListResponse(
            list,
            new List<ItemListSnapshotDbModel> { listValue },
            new List<ItemListItemActionDbModel>(),
            _itemsService);

        return listResponse;
    }

    public async Task<ErrorOr<ListResponse>> GetList(string? userId, string listUrl)
    {
        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.Public == false && list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: "You dont have access to this list");
        }

        var listInfos = _itemListRepo.GetListInfos(list.Value.Id);
        var listResponse = ItemListMapper.MapToListResponse(
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

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: "You dont have access to this list");
        }

        await _itemListRepo.DeleteList(list.Value.Id);
        return Result.Deleted;
    }

    public async Task<ErrorOr<Created>> BuyItem(
        string? userId,
        string listUrl,
        long itemId,
        decimal price,
        int amount)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
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

        await _itemListRepo.AddItemAction("B", list.Value, itemId, price, amount);
        await _itemListSnapshotRepo.CalculateWithLatestPrices(list.Value);
        return Result.Created;
    }

    public async Task<ErrorOr<Created>> SellItem(
        string? userId,
        string listUrl,
        long itemId,
        decimal price,
        int amount)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var listResult = await _itemListRepo.GetByUrl(listUrl);
        if (listResult.IsError)
        {
            return listResult.FirstError;
        }

        if (listResult.Value.UserId.Equals(userId) == false)
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

        var currentItemCount = await _itemListRepo.GetCurrentItemCount(listResult.Value, itemId);
        if (amount > currentItemCount)
        {
            return Error.Conflict(description:
                $"Cant sell {amount} items if the list \"{listUrl}\" only contains {currentItemCount} items with the id \"{itemId}\"");
        }

        await _itemListRepo.AddItemAction("S", listResult.Value, itemId, price, amount);
        await _itemListSnapshotRepo.CalculateWithLatestPrices(listResult.Value);
        return Result.Created;
    }

    public async Task<ErrorOr<Updated>> UpdateListName(string? userId, string listUrl, string newName)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.UpdateName(list.Value.Id, newName);
        return Result.Updated;
    }

    public async Task<ErrorOr<Updated>> UpdateListDescription(string? userId, string listUrl, string newDescription)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.UpdateDescription(list.Value.Id, newDescription);
        return Result.Updated;
    }

    public async Task<ErrorOr<Updated>> UpdateListPublic(string? userId, string listUrl, bool newPublic)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.UpdatePublic(list.Value.Id, newPublic);
        return Result.Updated;
    }
}