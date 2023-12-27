using application.Cache;
using ErrorOr;
using infrastructure.Database.Repos;
using infrastructure.Items;
using MediatR;

namespace application.Commands.List;

public record AddItemActionBuyCommand(
    string? UserId,
    string ListUrl,
    long ItemId,
    long UnitPrice,
    int Amount) : IRequest<ErrorOr<Created>>;

public class AddItemActionBuyHandler : IRequestHandler<AddItemActionBuyCommand, ErrorOr<Created>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;
    private readonly ItemsService _itemsService;

    public AddItemActionBuyHandler(
        UnitOfWork unitOfWork,
        ListResponseCacheService listResponseCacheService,
        ItemsService itemsService)
    {
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
        _itemsService = itemsService;
    }

    public async Task<ErrorOr<Created>> Handle(AddItemActionBuyCommand request, CancellationToken cancellationToken)
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
            return Error.Failure(description: $"Cant buy {request.Amount} items");
        }

        if (request.Amount >= 5000)
        {
            return Error.Failure(description: "Cant buy more then 5000 items at once");
        }

        await _unitOfWork.ItemListRepo.AddItemAction(
            "B",
            list.Value,
            request.ItemId,
            request.UnitPrice,
            request.Amount);

        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(request.ListUrl);
        return Result.Created;
    }
}