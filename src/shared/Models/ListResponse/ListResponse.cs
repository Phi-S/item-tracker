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
    [property: JsonRequired, JsonPropertyName("items")]
    List<ListItemResponse> Items,
    [property: JsonRequired, JsonPropertyName("snapshots")]
    List<ListSnapshotResponse> Snapshots
);