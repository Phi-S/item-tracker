using ErrorOr;
using infrastructure.Database.Repos;
using MediatR;
using shared.Models.ListResponse;

namespace application.Queries;

public record GetAllListsForUserQuery(string? UserId) : IRequest<ErrorOr<List<ListResponse>>>;

public class GetAllListsForUserHandlers : IRequestHandler<GetAllListsForUserQuery, ErrorOr<List<ListResponse>>>
{
    private readonly IMediator _mediator;
    private readonly UnitOfWork _unitOfWork;

    public GetAllListsForUserHandlers(IMediator mediator, UnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<List<ListResponse>>> Handle(GetAllListsForUserQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var result = new List<ListResponse>();
        var lists = await _unitOfWork.ItemListRepo.GetAllListsForUser(request.UserId);
        foreach (var list in lists)
        {
            if (list.UserId.Equals(request.UserId) == false)
            {
                return Error.Unauthorized(description: "You dont have access to this list");
            }

            var getListQuery = new GetListQuery(request.UserId, list.Url);
            var listResponse = await _mediator.Send(getListQuery, cancellationToken);
            if (listResponse.IsError)
            {
                return listResponse.FirstError;
            }

            result.Add(listResponse.Value);
        }

        return result;
    }
}