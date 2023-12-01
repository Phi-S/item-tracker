﻿using ErrorOr;
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
        if (userId is null)
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
        if (userId is null)
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

        var list = await _itemListRepo.New(
            userId,
            newListModel.ListName,
            newListModel.ListDescription,
            newListModel.Currency,
            newListModel.Public
        );

        var listResponse = ItemListMapper.MapToListResponse(
            list,
            new List<ItemListValueDbModel>(),
            new List<ItemListItemActionDbModel>(),
            _itemsService);

        return listResponse;
    }

    public async Task<ErrorOr<ListResponse>> Get(string? userId, string listUrl)
    {
        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.Public == false && list.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: "You dont have access to this list");
        }

        var listInfos = _itemListRepo.GetListInfos(listUrl);
        var listResponse = ItemListMapper.MapToListResponse(
            listInfos.list,
            listInfos.listValues,
            listInfos.items,
            _itemsService);
        return listResponse;
    }

    public async Task<ErrorOr<Deleted>> Delete(string? userId, string listUrl)
    {
        if (userId is null)
        {
            return Error.Unauthorized("UserId not found");
        }

        var listToDelete = await _itemListRepo.GetByUrl(listUrl);
        if (listToDelete.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: $"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.Delete(listToDelete.Id);
        return Result.Deleted;
    }

    public async Task<ErrorOr<Created>> BuyItem(
        string? userId,
        string listUrl,
        long itemId,
        decimal price,
        long amount)
    {
        if (userId is null)
        {
            return Error.Unauthorized("UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized($"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.BuyItem(list, itemId, price, amount);
        await _itemListValueRepo.CalculateLatest(list);
        return Result.Created;
    }

    public async Task<ErrorOr<Created>> SellItem(
        string? userId,
        string listUrl,
        long itemId,
        decimal price,
        long amount)
    {
        if (userId is null)
        {
            return Error.Unauthorized("UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized($"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.SellItem(list, itemId, price, amount);
        await _itemListValueRepo.CalculateLatest(list);
        return Result.Created;
    }

    public async Task<ErrorOr<Deleted>> DeleteItemAction(string? userId, string listUrl, long itemActionId)
    {
        if (userId is null)
        {
            return Error.Unauthorized("UserId not found");
        }

        var list = await _itemListRepo.GetByUrl(listUrl);
        if (list.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized($"The list \"{listUrl}\" dose not belong to the user \"{userId}\"");
        }

        await _itemListRepo.DeleteItem(itemActionId);
        await _itemListValueRepo.CalculateLatest(list);
        return Result.Deleted;
    }
}