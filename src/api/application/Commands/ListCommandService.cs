using ErrorOr;
using infrastructure.Currencies;
using infrastructure.Database.Models;
using infrastructure.Database.Repos;
using infrastructure.Items;
using infrastructure.Mapper;
using shared.Models;
using shared.Models.ListResponse;

namespace application.Commands;

public class ListCommandService
{
    private readonly ItemListRepo _itemListRepo;
    private readonly ItemListValueRepo _itemListValueRepo;
    private readonly ItemsService _itemsService;

    public ListCommandService(
        ItemListRepo itemListRepo,
        ItemListValueRepo itemListValueRepo,
        ItemsService itemsService)
    {
        _itemListRepo = itemListRepo;
        _itemListValueRepo = itemListValueRepo;
        _itemsService = itemsService;
    }


    public async Task<ErrorOr<List<ListResponse>>> GetAllForUser(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized("UserId not found");
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
            return Error.Unauthorized("UserId not found");
        }

        var isCurrencyValid = CurrenciesHelper.IsCurrencyValid(newListModel.Currency);
        if (isCurrencyValid == false)
        {
            return Error.Conflict(description: $"Currency \"{newListModel.Currency}\" is not a valid currency");
        }

        var existingListWithName = await _itemListRepo.ExistsWithNameForUser(userId, newListModel.ListName);
        if (existingListWithName)
        {
            return Error.Conflict(description: $"List with the name \"{newListModel.ListName}\" already exist");
        }

        // TODO: itemlistdbmodel as parameter and check if url exists before adding?
        var list = await _itemListRepo.New(
            userId,
            newListModel.ListName,
            newListModel.ListDescription,
            newListModel.Currency,
            newListModel.Public
        );

        var listValue = await _itemListValueRepo.CalculateLatest(list);
        var listResponse = ItemListMapper.MapToListResponse(
            list,
            new List<ItemListValueDbModel> { listValue },
            new List<ItemListItemActionDbModel>(),
            _itemsService);

        return listResponse;
    }

    public async Task<ErrorOr<ListResponse>> Get(string? userId, string listUrl)
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

    public async Task<ErrorOr<Deleted>> Delete(string? userId, string listUrl)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized("UserId not found");
        }

        var listToDelete = await _itemListRepo.GetByUrl(listUrl);
        if (listToDelete.IsError)
        {
            return listToDelete.FirstError;
        }

        if (listToDelete.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.Delete(listToDelete.Value.Id);
        return Result.Deleted;
    }

    public async Task<ErrorOr<Created>> BuyItem(
        string? userId,
        string listUrl,
        long itemId,
        decimal price,
        long amount)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized("UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized($"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.AddItemAction("B", list.Value, itemId, price, amount);
        await _itemListValueRepo.CalculateLatest(list.Value);
        return Result.Created;
    }

    public async Task<ErrorOr<Created>> SellItem(
        string? userId,
        string listUrl,
        long itemId,
        decimal price,
        long amount)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized("UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized($"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.AddItemAction("S", list.Value, itemId, price, amount);
        await _itemListValueRepo.CalculateLatest(list.Value);
        return Result.Created;
    }

    public async Task<ErrorOr<Deleted>> DeleteItemAction(string? userId, string listUrl, long itemActionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized("UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized($"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.DeleteItem(itemActionId);
        await _itemListValueRepo.CalculateLatest(list.Value);
        return Result.Deleted;
    }
}