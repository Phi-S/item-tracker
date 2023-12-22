using ErrorOr;
using shared.Models.ListResponse;

namespace application.Commands.List;

public partial class ListCommandService
{
    public async Task<ErrorOr<List<ListResponse>>> GetUserLists(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var result = new List<ListResponse>();
        var lists = await _unitOfWork.ItemListRepo.GetAllListsForUser(userId);
        foreach (var list in lists)
        {
            var cacheResponse = _listResponseCacheService.GetListResponse(list.Url);
            if (cacheResponse.IsError)
            {
                var listResponse = await GetListResponse(list.Url);
                if (listResponse.IsError)
                {
                    return listResponse.FirstError;
                }

                result.Add(listResponse.Value);
            }
            else
            {
                result.Add(cacheResponse.Value);
            }
        }

        return result;
    }

    public async Task<ErrorOr<ListResponse>> GetList(string? userId, string listUrl)
    {
        var list = await _unitOfWork.ItemListRepo.GetListByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.Public == false && list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: "You dont have access to this list");
        }

        var cachedListResponse = _listResponseCacheService.GetListResponse(listUrl);
        if (cachedListResponse.IsError == false)
        {
            return cachedListResponse.Value;
        }

        var listResponse = await GetListResponse(listUrl);
        if (listResponse.IsError)
        {
            return listResponse.FirstError;
        }

        _listResponseCacheService.UpdateCache(listResponse.Value);
        return listResponse;
    }
}