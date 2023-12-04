using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListItemActionResponse(
    [property: JsonRequired, JsonPropertyName("action_id")]
    long ActionId,
    [property: JsonRequired, JsonPropertyName("action")]
    string Action,
    [property: JsonRequired, JsonPropertyName("amount")]
    int Amount,
    [property: JsonRequired, JsonPropertyName("price")]
    decimal Price,
    [property: JsonRequired, JsonPropertyName("created_utc")]
    DateTime CreatedAt);

public record ListItemResponse(
    [property: JsonRequired, JsonPropertyName("item_id")]
    long ItemId,
    [property: JsonRequired, JsonPropertyName("item_name")]
    string ItemName,
    [property: JsonRequired, JsonPropertyName("item_image")]
    string ItemImage,
    [property: JsonRequired, JsonPropertyName("current_capital_invested")]
    decimal CurrentCapitalInvested,
    // CurrentAmountInvested == buyAmount - sellAmount
    [property: JsonRequired, JsonPropertyName("current_amount_invested")]
    int CurrentAmountInvested,
    // CurrentAverageBuyPrice == The average buy price since the last time the item amount was 0
    [property: JsonRequired, JsonPropertyName("current_average_buy_price")]
    decimal CurrentAverageBuyPrice,
    [property: JsonRequired, JsonPropertyName("actions")]
    List<ListItemActionResponse> Actions
);