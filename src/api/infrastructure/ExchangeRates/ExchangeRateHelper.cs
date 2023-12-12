namespace infrastructure.ExchangeRates;

public static class ExchangeRateHelper
{
    public static long ApplyExchangeRate(double exchangeRate, long value)
    {
        return (long)Math.Round(value * exchangeRate, 0);
    }
}