using shared.Currencies;

namespace presentation.Helper;

public static class CurrencyHelper
{
    public static long CurrencyToSmallestUnit(string currency, decimal value)
    {
        if (currency.Equals(CurrenciesConstants.EURO) || currency.Equals(CurrenciesConstants.USD))
        {
            return (long)Math.Round(value * 100, 2);
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