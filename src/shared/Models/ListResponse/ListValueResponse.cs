using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListValueResponse(
    [property: JsonRequired, JsonPropertyName("steamValue")]
    decimal? SteamValue,
    [property: JsonRequired, JsonPropertyName("buffValue")]
    decimal? BuffValue,
    [property: JsonRequired, JsonPropertyName("investedCapital")]
    decimal InvestedCapital,
    [property: JsonRequired, JsonPropertyName("createdAt")]
    DateTime CreatedAt
);