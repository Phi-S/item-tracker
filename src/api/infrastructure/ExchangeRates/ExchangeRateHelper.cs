namespace infrastructure.ExchangeRates;

public static class ExchangeRateHelper
{
    public static long ApplyExchangeRate(long value, double exchangeRate)
    {
        return (long)Math.Round(value * exchangeRate, 0);
    }
}