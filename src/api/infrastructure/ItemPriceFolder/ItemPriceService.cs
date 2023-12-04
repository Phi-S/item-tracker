using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ErrorOr;
using Error = ErrorOr.Error;

namespace infrastructure.ItemPriceFolder;

public class ItemPriceService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public ItemPriceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ErrorOr<(ProviderPricesModel steamPrices, ProviderPricesModel buff163Prices)>> GetPrices()
    {
        var steamPricesTask = GetSteamPrices();
        var buffPricesTask = GetBuffPrices();
        await Task.WhenAll(steamPricesTask, buffPricesTask);
        var steamPricesResult = steamPricesTask.Result;
        if (steamPricesResult.IsError)
        {
            return steamPricesResult.FirstError;
        }

        var buffPricesResult = buffPricesTask.Result;
        if (buffPricesResult.IsError)
        {
            return buffPricesResult.FirstError;
        }
        
        return (steamPricesResult.Value, buffPricesResult.Value);
    }

    private async Task<ErrorOr<ProviderPricesModel>> GetSteamPrices()
    {
        var pricesResponse = await GetPricesJson("steam");
        var prices =
            JsonSerializer.Deserialize<Dictionary<string, JsonObject>>(pricesResponse.json, JsonSerializerOptions);
        if (prices is null)
        {
            return Error.Failure("Failed to Deserialize price json");
        }

        var result = new List<(string itemName, decimal? price)>();
        foreach (var (name, priceJson) in prices)
        {
            var price = GetSteamPriceFromJson(priceJson);
            if (price.IsError)
            {
                return price.FirstError;
            }

            result.Add((name, price.Value));
        }

        return new ProviderPricesModel(pricesResponse.lastModified, result);
    }

    private async Task<ErrorOr<ProviderPricesModel>> GetBuffPrices()
    {
        var pricesResponse = await GetPricesJson("buff163");
        var prices =
            JsonSerializer.Deserialize<Dictionary<string, JsonObject>>(pricesResponse.json, JsonSerializerOptions);
        if (prices is null)
        {
            return Error.Failure("Failed to Deserialize price json");
        }

        var result = new List<(string itemName, decimal? price)>();
        foreach (var (name, priceJson) in prices)
        {
            var price = GetBuffPriceModelFromJson(priceJson);
            if (price.IsError)
            {
                return price.FirstError;
            }

            result.Add((name, price.Value));
        }

        return new ProviderPricesModel(pricesResponse.lastModified, result);
    }

    private async Task<(DateTime lastModified, string json)> GetPricesJson(string provider)
    {
        var response = await _httpClient.GetAsync($"https://prices.csgotrader.app/latest/{provider}.json");
        var lastModifiedString = response.Content.Headers
            .First(pair => pair.Key.Equals("last-modified", StringComparison.InvariantCultureIgnoreCase)).Value.First();
        var lastModified = DateTime.Parse(lastModifiedString).ToUniversalTime();
        var gzipStream = await response.Content.ReadAsStreamAsync();

        await using var zipStream = new GZipStream(gzipStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        await zipStream.CopyToAsync(resultStream);
        var resultBytes = resultStream.ToArray();
        var resultJson = Encoding.UTF8.GetString(resultBytes);
        return (lastModified, resultJson);
    }

    private static ErrorOr<decimal?> GetSteamPriceFromJson(JsonNode jsonObject)
    {
        var steamPriceLast24H = GetSteamPrice(jsonObject, "last_24h");
        if (steamPriceLast24H.IsError)
        {
            return steamPriceLast24H.FirstError;
        }

        if (steamPriceLast24H.Value is not null)
        {
            return steamPriceLast24H.Value.Value;
        }

        var steamPriceLast7d = GetSteamPrice(jsonObject, "last_7d");
        if (steamPriceLast7d.IsError)
        {
            return steamPriceLast7d.FirstError;
        }

        if (steamPriceLast7d.Value is not null)
        {
            return steamPriceLast7d.Value.Value;
        }

        var steamPriceLast30d = GetSteamPrice(jsonObject, "last_30d");
        if (steamPriceLast30d.IsError)
        {
            return steamPriceLast30d.FirstError;
        }

        if (steamPriceLast30d.Value is not null)
        {
            return steamPriceLast30d.Value.Value;
        }

        var steamPriceLast90d = GetSteamPrice(jsonObject, "last_90d");
        if (steamPriceLast90d.IsError)
        {
            return steamPriceLast90d.FirstError;
        }

        return steamPriceLast90d.Value;

        ErrorOr<decimal?> GetSteamPrice(JsonNode jsonNode, string jsonPropertyName)
        {
            foreach (var node in jsonNode.AsObject())
            {
                if (node.Key.Equals(jsonPropertyName) == false)
                {
                    continue;
                }

                if (node.Value is null)
                {
                    return (decimal?)null;
                }
            }

            var priceJson = jsonNode[jsonPropertyName]?.ToString();
            if (string.IsNullOrWhiteSpace(priceJson) ||
                decimal.TryParse(priceJson, NumberStyles.Float, CultureInfo.InvariantCulture, out var steamPrice) ==
                false)
            {
                return Error.Failure($"Item dose not have steam price for \"{jsonPropertyName}\"");
            }

            return steamPrice;
        }
    }

    private static ErrorOr<decimal?> GetBuffPriceModelFromJson(JsonObject jsonObject)
    {
        ErrorOr<decimal?> GetBuffPrice(JsonNode jsonNode, string jsonPropertyName)
        {
            foreach (var node in jsonNode.AsObject())
            {
                if (node.Key.Equals(jsonPropertyName) == false)
                {
                    continue;
                }

                if (node.Value is null)
                {
                    return (decimal?)null;
                }

                if (node.Value.AsObject().First().Key.Equals("price") &&
                    node.Value.AsObject().First().Value is null)
                {
                    return (decimal?)null;
                }
            }

            var priceString = jsonNode[jsonPropertyName]?["price"]?.ToString();
            if (string.IsNullOrWhiteSpace(priceString) ||
                decimal.TryParse(priceString, NumberStyles.Float, CultureInfo.InvariantCulture, out var price) == false)
            {
                return Error.Failure($"Item dose not have buff price for \"{jsonPropertyName}\"");
            }

            return price;
        }

        var buffPriceStartingAt = GetBuffPrice(jsonObject, "starting_at");
        if (buffPriceStartingAt.IsError)
        {
            return buffPriceStartingAt.FirstError;
        }

        if (buffPriceStartingAt.Value is not null)
        {
            return buffPriceStartingAt.Value.Value;
        }

        var buffPriceHighestOrder = GetBuffPrice(jsonObject, "highest_order");
        if (buffPriceHighestOrder.IsError)
        {
            return buffPriceHighestOrder.FirstError;
        }

        return buffPriceHighestOrder.Value;
    }
}