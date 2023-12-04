using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListSnapshotResponse(
    [property: JsonRequired, JsonPropertyName("invested_capital")]
    decimal InvestedCapital,
    [property: JsonRequired, JsonPropertyName("item_count")]
    int ItemCount,
    [property: JsonRequired, JsonPropertyName("steam_value")]
    decimal? SteamValue,
    [property: JsonRequired, JsonPropertyName("buff163_value")]
    decimal? Buff163Value,
    [property: JsonRequired, JsonPropertyName("created_at")]
    DateTime CreatedAt
);