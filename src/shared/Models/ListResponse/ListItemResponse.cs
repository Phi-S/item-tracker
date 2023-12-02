using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListItemActionResponse(
    [property: JsonRequired, JsonPropertyName("actionId")]
    long ActionId,
    [property: JsonRequired, JsonPropertyName("action")]
    string Action,
    [property: JsonRequired, JsonPropertyName("amount")]
    long Amount,
    [property: JsonRequired, JsonPropertyName("pricePerOne")]
    decimal PricePerOne,
    [property: JsonRequired, JsonPropertyName("createdUtc")]
    DateTime CreatedAt);

public record ListItemResponse(
    [property: JsonRequired, JsonPropertyName("itemId")]
    long ItemId,
    [property: JsonRequired, JsonPropertyName("itemName")]
    string ItemName,
    [property: JsonRequired, JsonPropertyName("itemImage")]
    string ItemImage,
    [property: JsonRequired, JsonPropertyName("totalBuyAmount")]
    decimal TotalBuyAmount,
    [property: JsonRequired, JsonPropertyName("totalBuyPrice")]
    decimal TotalBuyPrice,
    [property: JsonRequired, JsonPropertyName("averageBuyPrice")]
    decimal AverageBuyPrice,
    [property: JsonRequired, JsonPropertyName("totalSellAmount")]
    decimal TotalSellAmount,
    [property: JsonRequired, JsonPropertyName("totalSellPrice")]
    decimal TotalSellPrice,
    [property: JsonRequired, JsonPropertyName("averageSellPrice")]
    decimal AverageSellPrice,
    [property: JsonRequired, JsonPropertyName("actions")]
    List<ListItemActionResponse> Actions
);