using System.Text.Json.Serialization;

namespace shared.Models;

public record ItemSearchResponse(
    [property: JsonPropertyName("id")]long Id,
    [property: JsonPropertyName("name")]string Name,
    [property: JsonPropertyName("image")]string Image
    );