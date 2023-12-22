using ErrorOr;
using shared.Models.ListResponse;
using Error = ErrorOr.Error;

namespace application.Cache;

public class ListResponseCacheService
{
    private static readonly Dictionary<string, (DateOnly cacheDay, ListResponse listResponse)> ListResponses = new();
    private static readonly object ListResponsesLock = new();

    private static DateOnly GetCurrentDay()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public ErrorOr<ListResponse> GetListResponse(string listUrl)
    {
        lock (ListResponsesLock)
        {
            if (ListResponses.TryGetValue(listUrl, out var listResponse) == false)
            {
                return Error.NotFound();
            }

            if (listResponse.cacheDay == GetCurrentDay())
            {
                return listResponse.listResponse;
            }

            return Error.Conflict();
        }
    }

    public void UpdateCache(ListResponse listResponse)
    {
        lock (ListResponsesLock)
        {
            ListResponses[listResponse.Url] = (GetCurrentDay(), listResponse);
        }
    }

    public void DeleteCache(string listUrl)
    {
        lock (ListResponsesLock)
        {
            ListResponses.Remove(listUrl);
        }
    }

    public void DeleteCache()
    {
        lock (ListResponsesLock)
        {
            ListResponses.Clear();
        }
    }
}