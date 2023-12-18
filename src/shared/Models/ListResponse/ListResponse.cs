using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListResponse(
    [property: JsonRequired, JsonPropertyName("name")]
    string Name,
    [property: JsonRequired, JsonPropertyName("description")]
    string? Description,
    [property: JsonRequired, JsonPropertyName("url")]
    string Url,
    [property: JsonRequired, JsonPropertyName("currency")]
    string Currency,
    [property: JsonRequired, JsonPropertyName("public")]
    bool Public,
    [property: JsonRequired, JsonPropertyName("userId")]
    string UserId,
    [property: JsonRequired, JsonPropertyName("item_count")]
    int ItemCount,
    [property: JsonRequired, JsonPropertyName("invested_capital")]
    long InvestedCapital,
    [property: JsonRequired, JsonPropertyName("steam_sell_price")]
    long SteamSellPrice,
    [property: JsonRequired, JsonPropertyName("buff163_sell_price")]
    long Buff163SellPrice,
    [property: JsonRequired, JsonPropertyName("steam_performance_percent")]
    double? SteamPerformancePercent,
    [property: JsonRequired, JsonPropertyName("buff163_performance_percent")]
    double? Buff163PerformancePercent,
    [property: JsonRequired, JsonPropertyName("steam_performance_value")]
    long SteamPerformanceValue,
    [property: JsonRequired, JsonPropertyName("buff163_performance_value")]
    long Buff163PerformanceValue,
    [property: JsonRequired, JsonPropertyName("items")]
    List<ListItemResponse> Items,
    [property: JsonRequired, JsonPropertyName("snapshots")]
    List<ListSnapshotResponse> Snapshots
);