namespace presentation.Helper;

public static class CurrencyHelper
{
    public static string FormatCurrency(string currencyString, decimal value)
    {
        value = Math.Round(value, 2);
        if (currencyString.Equals("EUR"))
        {
            return $"{value}€";
        }

        if (currencyString.Equals("USD"))
        {
            return $"${value}";
        }

        throw new Exception($"\"{currencyString}\" is not a valid currency");
    }
}