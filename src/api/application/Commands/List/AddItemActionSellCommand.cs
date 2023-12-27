using application.Cache;
using ErrorOr;
using infrastructure.Database.Repos;
using infrastructure.Items;
using MediatR;

namespace application.Commands.List;

public record AddItemActionSellCommand(
    string? UserId,
    string ListUrl,
    long ItemId,
    long UnitPrice,
    int Amount
) : IRequest<ErrorOr<Created>>;

public class AddItemActionSellHandler : IRequestHandler<AddItemActionSellCommand, ErrorOr<Created>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;
    private readonly ItemsService _itemsService;

    public AddItemActionSellHandler(
        UnitOfWork unitOfWork,
        ListResponseCacheService listResponseCacheService,
        ItemsService itemsService)
    {
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
        _itemsService = itemsService;
    }

    public async Task<ErrorOr<Created>> Handle(AddItemActionSellCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var item = _itemsService.GetById(request.ItemId);
        if (item.IsError)
        {
            return item.FirstError;
        }

        var list = await _unitOfWork.ItemListRepo.GetListByUrl(request.ListUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(request.UserId) == false)
        {
            return Error.Unauthorized(
                description: $"The list \"{request.ListUrl}\" dose not belong to the user \"{request.UserId}\"");
        }

        if (request.Amount <= 0)
        {
            return Error.Failure(description: $"Cant sell {request.Amount} items");
        }

        if (request.Amount >= 5000)
        {
            return Error.Failure(description: "Cant sell more then 5000 items at once");
        }

        var currentItemCount = await _unitOfWork.ItemListRepo.GetListItemCount(list.Value.Id, request.ItemId);
        if (request.Amount > currentItemCount)
        {
            return Error.Conflict(description:
                $"Cant sell {request.Amount} items if the list \"{request.ListUrl}\" only contains {currentItemCount} items with the id \"{request.ItemId}\"");
        }

        await _unitOfWork.ItemListRepo.AddItemAction(
            "S",
            list.Value,
            request.ItemId,
            request.UnitPrice,
            request.Amount);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(request.ListUrl);
        return Result.Created;
    }
}