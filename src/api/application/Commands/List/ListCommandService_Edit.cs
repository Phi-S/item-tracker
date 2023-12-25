using ErrorOr;
using shared.Currencies;
using shared.Models;

namespace application.Commands.List;

public partial class ListCommandService
{
    public async Task<ErrorOr<string>> New(string? userId, NewListModel newListModel)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var isCurrencyValid =
            CurrenciesConstants.ValidCurrencies.Any(currency => currency.Equals(newListModel.Currency));
        if (isCurrencyValid == false)
        {
            return Error.Failure(description: $"Currency \"{newListModel.Currency}\" is not a valid currency");
        }

        var existingListWithName = await _unitOfWork.ItemListRepo.ListNameTakenForUser(userId, newListModel.ListName);
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

        var latestPriceRefresh = await _unitOfWork.ItemPriceRepo.GetLatest();
        if (latestPriceRefresh.IsError)
        {
            return latestPriceRefresh.FirstError;
        }

        await _unitOfWork.Save();
        return url;
    }

    public async Task<ErrorOr<Updated>> UpdateListName(string? userId, string listUrl, string newName)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
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

        await _unitOfWork.ItemListRepo.UpdateListName(list.Value.Id, newName);
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

        await _unitOfWork.ItemListRepo.UpdateListDescription(list.Value.Id, newDescription);
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

        await _unitOfWork.ItemListRepo.UpdateListPublicState(list.Value.Id, newPublic);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(listUrl);
        return Result.Updated;
    }
}