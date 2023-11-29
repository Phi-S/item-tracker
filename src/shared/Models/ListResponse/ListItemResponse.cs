using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListItemResponse(
    [property: JsonRequired, JsonPropertyName("id")]long Id,
    [property: JsonRequired, JsonPropertyName("itemId")]long ItemId,
    [property: JsonRequired, JsonPropertyName("itemName")]string ItemName,
    [property: JsonRequired, JsonPropertyName("itemImage")]string ItemImage,
    [property: JsonRequired, JsonPropertyName("action")]string Action,
    [property: JsonRequired, JsonPropertyName("pricePerOne")]decimal PricePerOne,
    [property: JsonRequired, JsonPropertyName("amount")]long Amount,
    [property: JsonRequired, JsonPropertyName("createdUtc")]DateTime CreatedUtc
);