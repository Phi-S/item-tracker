using ErrorOr;
using infrastructure.Items;
using MediatR;
using shared.Models;

namespace application.Queries;

public record SearchItemQuery(string SearchString) : IRequest<ErrorOr<IEnumerable<ItemSearchResponse>>>;

public class SearchItemQueryHandler : IRequestHandler<SearchItemQuery, ErrorOr<IEnumerable<ItemSearchResponse>>>
{
    private readonly ItemsService _itemsService;

    public SearchItemQueryHandler(ItemsService itemsService)
    {
        _itemsService = itemsService;
    }
    
    public Task<ErrorOr<IEnumerable<ItemSearchResponse>>> Handle(SearchItemQuery request, CancellationToken cancellationToken)
    {
        var search = _itemsService.Search(request.SearchString);
        if (search.IsError)
        {
            return Task.FromResult<ErrorOr<IEnumerable<ItemSearchResponse>>>(search.FirstError);
        }

        return Task.FromResult<ErrorOr<IEnumerable<ItemSearchResponse>>>(search.Value.Select(model => new ItemSearchResponse(model.Id, model.Name, model.Image)).ToList());
    }
}