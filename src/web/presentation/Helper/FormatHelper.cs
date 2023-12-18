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

    public static string FormatCurrency(string currency, long? value, bool indicatePositiveValue = false) =>
        FormatCurrency(currency, value ?? 0);

    public static string FormatCurrency(string currency, long value, bool indicatePositiveValue = false)
    {
        var valueAsDouble = CurrencyHelper.ToDouble(currency, value);
        var positivePrefix = indicatePositiveValue && valueAsDouble > 0 ? "+" : "";
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