using application.Cache;
using ErrorOr;
using infrastructure.Database.Repos;
using MediatR;

namespace application.Commands.List;

public record UpdateListPublicCommand(string? UserId, string ListUrl, bool NewPublic) : IRequest<ErrorOr<Updated>>;

public class UpdateListPublicCommandHandler : IRequestHandler<UpdateListPublicCommand, ErrorOr<Updated>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;

    public UpdateListPublicCommandHandler(UnitOfWork unitOfWork, ListResponseCacheService listResponseCacheService)
    {
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
    }
    
    public async Task<ErrorOr<Updated>> Handle(UpdateListPublicCommand request, CancellationToken cancellationToken)
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

        await _unitOfWork.ItemListRepo.UpdateListPublicState(list.Value.Id, request.NewPublic);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(request.ListUrl);
        return Result.Updated;
    }
}