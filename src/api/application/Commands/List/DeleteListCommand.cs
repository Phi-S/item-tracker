using application.Cache;
using ErrorOr;
using infrastructure.Database.Repos;
using MediatR;

namespace application.Commands.List;

public record DeleteListCommand(string? UserId, string ListUrl) : IRequest<ErrorOr<Deleted>>;

public class DeleteListCommandHandler : IRequestHandler<DeleteListCommand, ErrorOr<Deleted>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;

    public DeleteListCommandHandler(UnitOfWork unitOfWork, ListResponseCacheService listResponseCacheService)
    {
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteListCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _unitOfWork.ItemListRepo.GetListByUrl(request.ListUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(request.UserId) == false)
        {
            return Error.Unauthorized(description: "You dont have access to this list");
        }

        await _unitOfWork.ItemListRepo.DeleteList(list.Value.Id);
        _listResponseCacheService.DeleteCache(request.ListUrl);
        await _unitOfWork.Save();
        return Result.Deleted;
    }
}