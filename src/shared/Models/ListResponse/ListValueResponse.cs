using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListValueResponse(
    [property: JsonRequired, JsonPropertyName("value")]
    decimal Value,
    [property: JsonRequired, JsonPropertyName("createdAt")]
    DateTime CreatedAt
);