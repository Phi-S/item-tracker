using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using ErrorOr;
using Microsoft.Extensions.Configuration;
using shared.Currencies;
using Throw;

namespace infrastructure.ExchangeRates;

public class ExchangeRatesService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ExchangeRatesService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        var apiKey = configuration.GetValue<string>("ExchangeRatesApiKey");
        apiKey.ThrowIfNull().IfEmpty().IfWhiteSpace();
        _apiKey = apiKey;
    }

    public async Task<ErrorOr<double>> GetUsdEurExchangeRate()
    {
        var validCurrencies = string.Join(",", CurrenciesConstants.ValidCurrencies);
        var url =
            $"http://api.exchangeratesapi.io/v1/latest?access_key={_apiKey}&symbols={validCurrencies}&base=EUR";
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return Error.Failure(
                description: $"Http request to exchangeratesapi.io failed with status code {response.StatusCode}");
        }

        var responseString = await response.Content.ReadAsStringAsync();
        var responseJson = JsonSerializer.Deserialize<JsonObject>(responseString);
        if (responseJson is null)
        {
            return Error.Failure(description: $"Failed to deserialize response json. \n {responseString}");
        }

        var success = responseJson["success"]?.ToString();
        if (string.IsNullOrWhiteSpace(success) || success.ToLower().Trim().Equals("false"))
        {
            return Error.Failure(description: $"Call to exchangeratesapi.io return success false. \n {responseString}");
        }

        var rates = responseJson["rates"];
        if (rates is null)
        {
            return Error.Failure(description: $"Failed to get rates from response. \n {responseString}");
        }

        var usdRateString = rates["USD"]?.ToString();
        if (string.IsNullOrWhiteSpace(usdRateString))
        {
            return Error.Failure(description:
                $"Failed to get USD exchange rate from response. \n {responseString}");
        }

        var usdRateParse = double.TryParse(usdRateString, NumberStyles.Float, new NumberFormatInfo(), out var usdRate);
        if (usdRateParse == false)
        {
            return Error.Failure(description:
                $"Failed to parse usd rate string \"{usdRateString}\"");
        }

        return 1 / usdRate;
    }
}