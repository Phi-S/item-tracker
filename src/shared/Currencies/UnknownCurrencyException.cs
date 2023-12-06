namespace shared.Currencies;

public class UnknownCurrencyException : Exception
{
    public UnknownCurrencyException(string currency) : base($"Unknown currency: \"{currency}\"")
    {
    }
}