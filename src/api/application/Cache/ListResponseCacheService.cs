using ErrorOr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using shared.Models.ListResponse;
using Error = ErrorOr.Error;

namespace application.Cache;

public class ListResponseCacheService
{
    private static readonly Dictionary<string, (DateOnly cacheDay, ListResponse listResponse)> ListResponses = new();
    private static readonly object ListResponsesLock = new();

    private readonly bool _cacheEnabled = true;

    public ListResponseCacheService(ILogger<ListResponseCacheService> logger, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("DisableCache"))
        {
            _cacheEnabled = false;
            logger.LogWarning("ListResponseCache is disabled");
        }
    }

    private static DateOnly GetCurrentDay()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public ErrorOr<ListResponse> GetListResponse(string listUrl)
    {
        if (_cacheEnabled == false)
        {
            return Error.NotFound();
        }

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

            DeleteCache(listUrl);
            return Error.Conflict();
        }
    }

    public void UpdateCache(ListResponse listResponse)
    {
        if (_cacheEnabled == false)
        {
            return;
        }

        lock (ListResponsesLock)
        {
            ListResponses[listResponse.Url] = (GetCurrentDay(), listResponse);
        }
    }

    public void DeleteCache(string listUrl)
    {
        if (_cacheEnabled == false)
        {
            return;
        }

        lock (ListResponsesLock)
        {
            ListResponses.Remove(listUrl);
        }
    }

    public void DeleteCache()
    {
        if (_cacheEnabled == false)
        {
            return;
        }

        lock (ListResponsesLock)
        {
            ListResponses.Clear();
        }
    }
}