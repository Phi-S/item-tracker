using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ErrorOr;

namespace infrastructure.ItemPriceFolder;

public class ItemPriceService
{
    private readonly HttpClient _httpClient;

    public ItemPriceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ErrorOr<List<PriceModel>>> GetPrices()
    {
        var steamPricesJson = await GetPricesJson();
        var priceModelsFromJson = GetPriceModelsFromJson(steamPricesJson);
        return priceModelsFromJson;
    }

    private async Task<string> GetPricesJson()
    {
        var gzipStream = await _httpClient.GetStreamAsync(
            "https://prices.csgotrader.app/latest/prices_v6.json");

        await using var zipStream = new GZipStream(gzipStream, CompressionMode.Decompress);
        using var resultStream = new MemoryStream();
        await zipStream.CopyToAsync(resultStream);
        var resultBytes = resultStream.ToArray();
        var resultJson = Encoding.UTF8.GetString(resultBytes);
        return resultJson;
    }

    private static ErrorOr<List<PriceModel>>
        GetPriceModelsFromJson(string json)
    {
        var result = new List<PriceModel>();
        var prices = JsonSerializer.Deserialize<Dictionary<string, JsonObject>>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (prices is null)
        {
            return Error.Failure("Failed to Deserialize price json");
        }

        foreach (var (name, priceJson) in prices)
        {
            var steamPrice = GetSteamPriceModelFromJson(priceJson);
            if (steamPrice.IsError)
            {
                return Error.Failure(
                    description:
                    $"Failed to get steam price for item \"{name}\". {steamPrice.FirstError} | raw json: \\n {priceJson.ToJsonString()}\"");
            }

            var buffPrice = GetBuffPriceModelFromJson(priceJson);
            if (buffPrice.IsError)
            {
                return Error.Failure(
                    description:
                    $"Failed to get buff price for item \"{name}\". {buffPrice.FirstError} | raw json: \\n {priceJson.ToJsonString()}\"");
            }

            var price = new PriceModel(name, steamPrice.Value, buffPrice.Value);
            result.Add(price);
        }

        return result;
    }

    private static ErrorOr<decimal?> GetSteamPriceModelFromJson(JsonNode jsonObject)
    {
        var steamPriceJson = jsonObject["steam"];
        if (steamPriceJson is null)
        {
            return Error.Failure(description:
                $"Item dose not have an steam price part in json. Json: {jsonObject.ToJsonString()}");
        }

        var steamPriceLast24H = GetSteamPrice(steamPriceJson, "last_24h");
        if (steamPriceLast24H.IsError)
        {
            return steamPriceLast24H.FirstError;
        }

        if (steamPriceLast24H.Value is not null)
        {
            return steamPriceLast24H.Value.Value;
        }

        var steamPriceLast7d = GetSteamPrice(steamPriceJson, "last_7d");
        if (steamPriceLast7d.IsError)
        {
            return steamPriceLast7d.FirstError;
        }

        if (steamPriceLast7d.Value is not null)
        {
            return steamPriceLast7d.Value.Value;
        }

        var steamPriceLast30d = GetSteamPrice(steamPriceJson, "last_30d");
        if (steamPriceLast30d.IsError)
        {
            return steamPriceLast30d.FirstError;
        }

        if (steamPriceLast30d.Value is not null)
        {
            return steamPriceLast30d.Value.Value;
        }

        var steamPriceLast90d = GetSteamPrice(steamPriceJson, "last_90d");
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
                return Error.Failure($"Item dose not have steam price for \"{jsonPropertyName}\"");
            }

            return price;
        }

        var buffPriceJson = jsonObject["buff163"];
        if (buffPriceJson is null)
        {
            return Error.Failure(description:
                $"Item dose not have an buff price part in json. Json: {jsonObject.ToJsonString()}");
        }

        var buffPriceStartingAt = GetBuffPrice(buffPriceJson, "starting_at");
        if (buffPriceStartingAt.IsError)
        {
            return buffPriceStartingAt.FirstError;
        }

        if (buffPriceStartingAt.Value is not null)
        {
            return buffPriceStartingAt.Value.Value;
        }

        var buffPriceHighestOrder = GetBuffPrice(buffPriceJson, "highest_order");
        if (buffPriceHighestOrder.IsError)
        {
            return buffPriceHighestOrder.FirstError;
        }

        return buffPriceHighestOrder.Value;
    }
}