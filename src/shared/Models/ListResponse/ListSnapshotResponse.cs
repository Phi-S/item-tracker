using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListSnapshotResponse(
    [property: JsonRequired, JsonPropertyName("invested_capital")]
    long InvestedCapital,
    [property: JsonRequired, JsonPropertyName("item_count")]
    long ItemCount,
    [property: JsonRequired, JsonPropertyName("sales_value")]
    long SalesValue,
    [property: JsonRequired, JsonPropertyName("profit")]
    long Profit,
    [property: JsonRequired, JsonPropertyName("steam_sell_price")]
    long? SteamSellPrice,
    [property: JsonRequired, JsonPropertyName("buff163_sell_price")]
    long? Buff163SellPrice,
    [property: JsonRequired, JsonPropertyName("created_utc")]
    DateTime CreatedUtc
);