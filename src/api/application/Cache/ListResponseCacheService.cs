using ErrorOr;
using shared.Models.ListResponse;
using Error = ErrorOr.Error;

namespace application.Cache;

public class ListResponseCacheService
{
    private static readonly Dictionary<string, ListResponse> ListResponses = new();
    private static readonly object ListResponsesLock = new();

    public ErrorOr<ListResponse> GetListResponse(string listUrl)
    {
        lock (ListResponsesLock)
        {
            if (ListResponses.TryGetValue(listUrl, out var listResponse))
            {
                return listResponse;
            }
        }

        return Error.NotFound();
    }

    public void UpdateCache(ListResponse listResponse)
    {
        lock (ListResponsesLock)
        {
            ListResponses[listResponse.Url] = listResponse;
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