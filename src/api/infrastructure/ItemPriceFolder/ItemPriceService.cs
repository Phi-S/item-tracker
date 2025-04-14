using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
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
}