using application.Cache;
using ErrorOr;
using infrastructure.Database.Repos;
using MediatR;

namespace application.Commands.List;

public record UpdateListNameCommand(string? UserId, string ListUrl, string NewName) : IRequest<ErrorOr<Updated>>;

public class UpdateListNameCommandHandler : IRequestHandler<UpdateListNameCommand, ErrorOr<Updated>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;

    public UpdateListNameCommandHandler(UnitOfWork unitOfWork, ListResponseCacheService listResponseCacheService)
    {
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
    }
    
    public async Task<ErrorOr<Updated>> Handle(UpdateListNameCommand request, CancellationToken cancellationToken)
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
            return Error.Unauthorized(
                description: $"The list \"{request.ListUrl}\" dose not belong to the user \"{request.UserId}\"");
        }

        await _unitOfWork.ItemListRepo.UpdateListName(list.Value.Id, request.NewName);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(request.ListUrl);
        return Result.Updated;
    }
}

