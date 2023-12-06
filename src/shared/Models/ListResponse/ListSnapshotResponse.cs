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
    [property: JsonRequired, JsonPropertyName("steam_value")]
    long? SteamValue,
    [property: JsonRequired, JsonPropertyName("buff163_value")]
    long? Buff163Value,
    [property: JsonRequired, JsonPropertyName("created_at")]
    DateTime CreatedAt
);