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
    long Price,
    [property: JsonRequired, JsonPropertyName("created_utc")]
    DateTime CreatedAt);

public record ListItemResponse(
    [property: JsonRequired, JsonPropertyName("item_id")]
    long ItemId,
    [property: JsonRequired, JsonPropertyName("item_name")]
    string ItemName,
    [property: JsonRequired, JsonPropertyName("item_image")]
    string ItemImage,
    [property: JsonRequired, JsonPropertyName("capital_invested")]
    long CapitalInvested,
    // CurrentAmountInvested == buyAmount - sellAmount
    [property: JsonRequired, JsonPropertyName("amount_invested")]
    int AmountInvested,
    // CurrentAverageBuyPrice == The average buy price since the last time the item amount was 0
    [property: JsonRequired, JsonPropertyName("average_buy_price")]
    long AverageBuyPrice,
    [property: JsonRequired, JsonPropertyName("sales_value")]
    long SalesValue,
    [property: JsonRequired, JsonPropertyName("profit")]
    long Profit,
    [property: JsonRequired, JsonPropertyName("actions")]
    List<ListItemActionResponse> Actions
);