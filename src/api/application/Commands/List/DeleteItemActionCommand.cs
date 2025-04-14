using application.Cache;
using ErrorOr;
using infrastructure.Database.Repos;
using MediatR;

namespace application.Commands.List;

public record DeleteItemActionCommand(string? UserId, long ItemActionId) : IRequest<ErrorOr<Deleted>>;

public class DeleteItemActionCommandHandler : IRequestHandler<DeleteItemActionCommand, ErrorOr<Deleted>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;

    public DeleteItemActionCommandHandler(UnitOfWork unitOfWork, ListResponseCacheService listResponseCacheService)
    {
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteItemActionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var action = await _unitOfWork.ItemListRepo.GetItemActionById(request.ItemActionId);
        if (action.ListId.UserId.Equals(request.UserId) == false)
        {
            return Error.Unauthorized(
                description: $"The list \"{action.ListId.Url}\" dose not belong to the user \"{request.UserId}\"");
        }

        await _unitOfWork.ItemListRepo.DeleteItemAction(action.ListId, request.ItemActionId);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(action.ListId.Url);
        return Result.Deleted;
    }
}