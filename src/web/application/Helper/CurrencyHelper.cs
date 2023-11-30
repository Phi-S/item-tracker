namespace application.Helper;

public static class CurrencyHelper
{
    public static string Get(string currencyString)
    {
        if (currencyString.Equals("EUR"))
        {
            return "€";
        }

        if (currencyString.Equals("USD"))
        {
            return "$";
        }

        throw new Exception($"\"{currencyString}\" is not a valid currency");
    }

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