using shared.Models;
using shared.Models.ListResponse;

namespace application.Helper;

public static class ListResponseHelper
{
    public static ListValueResponse GetLatestListValue(ListResponse listResponse)
    {
        var latestListValue = listResponse.ListValues.OrderByDescending(list => list.CreatedAt).First();
        return latestListValue;
    }
}