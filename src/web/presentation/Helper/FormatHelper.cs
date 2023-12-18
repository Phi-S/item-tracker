using System.Globalization;
using shared.Currencies;

namespace presentation.Helper;

public static class FormatHelper
{
    public static string FormatPerformancePercent(double? performance)
    {
        if (performance is null)
        {
            return "---%";
        }

        return performance > 0 ? $"+{performance}%" : $"{performance}%";
    }

    public static string FormatCurrency(string currency, long? value, bool indicatePositiveValue = false)
    {
        var positivePrefix = indicatePositiveValue && value > 0 ? "+" : "";
        var valueAsDouble = value is null
            ? "---"
            : CurrencyHelper.ToDouble(currency, value.Value).ToString(CultureInfo.InvariantCulture);
        if (currency.Equals("EUR"))
        {
            return $"{positivePrefix}{valueAsDouble}€";
        }

        if (currency.Equals("USD"))
        {
            return $"${positivePrefix}{valueAsDouble}";
        }

        throw new UnknownCurrencyException(currency);
    }
}