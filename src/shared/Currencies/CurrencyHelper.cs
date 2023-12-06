namespace shared.Currencies;

public static class CurrencyHelper
{
    public static bool IsCurrencyValid(string currency)
    {
        return CurrenciesConstants.ValidCurrencies.Any(s => s.Equals(currency));
    }

    public static long CurrencyToSmallestUnit(string currency, decimal value)
    {
        if (currency.Equals(CurrenciesConstants.EURO) || currency.Equals(CurrenciesConstants.USD))
        {
            return (long)Math.Round(value * 100, 2);
        }

        throw new UnknownCurrencyException(currency);
    }
    
    public static string FormatCurrency(string currency, long value)
    {
        if (currency.Equals("EUR"))
        {
            return $"{(double)value / 100}€";
        }

        if (currency.Equals("USD"))
        {
            return $"${(double)value / 100}";
        }

        throw new UnknownCurrencyException(currency);
    }

    public static double ToDouble(string currency, long value)
    {
        if (currency.Equals(CurrenciesConstants.EURO) || currency.Equals(CurrenciesConstants.USD))
        {
            return Math.Round((double)value / 100, 2);
        }
        throw new UnknownCurrencyException(currency);
    }
}