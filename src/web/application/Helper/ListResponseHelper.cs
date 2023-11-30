using shared.Models.ListResponse;

namespace application.Helper;

public static class ListResponseHelper
{
    public static string GetCurrentValue(ListResponse listResponse)
    {
        var latestListValue = listResponse.ListValues.MaxBy(list => list.CreatedAt);
        return CurrencyHelper.FormatCurrency(
            listResponse.Currency,
            latestListValue?.SteamValue ?? 0
        );
    }
}