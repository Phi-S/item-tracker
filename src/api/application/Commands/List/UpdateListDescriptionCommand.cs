using application.Cache;
using ErrorOr;
using infrastructure.Database.Repos;
using MediatR;

namespace application.Commands.List;

public record UpdateListDescriptionCommand(string? UserId, string ListUrl, string NewDescription)
    : IRequest<ErrorOr<Updated>>;

public class UpdateListDescriptionCommandHandler : IRequestHandler<UpdateListDescriptionCommand, ErrorOr<Updated>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;

    public UpdateListDescriptionCommandHandler(UnitOfWork unitOfWork, ListResponseCacheService listResponseCacheService)
    {
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
    }

    public async Task<ErrorOr<Updated>> Handle(UpdateListDescriptionCommand request,
        CancellationToken cancellationToken)
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

        await _unitOfWork.ItemListRepo.UpdateListDescription(list.Value.Id, request.NewDescription);
        await _unitOfWork.Save();
        _listResponseCacheService.DeleteCache(request.ListUrl);
        return Result.Updated;
    }
}