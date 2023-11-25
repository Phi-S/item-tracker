namespace infrastructure.Currencies;

public static class CurrenciesHelper
{
    public static bool IsCurrencyValid(string currency)
    {
        return CurrenciesConstants.ValidCurrencies.Any(s => s.Equals(currency));
    }
}