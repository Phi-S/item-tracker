using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListItemResponse(
    [property: JsonRequired, JsonPropertyName("item_id")]
    long ItemId,
    [property: JsonRequired, JsonPropertyName("item_name")]
    string ItemName,
    [property: JsonRequired, JsonPropertyName("item_image")]
    string ItemImage,
    [property: JsonRequired, JsonPropertyName("invested_capital")]
    long InvestedCapital,
    // CurrentAmountInvested == buyAmount - sellAmount
    [property: JsonRequired, JsonPropertyName("item_count")]
    int ItemCount,
    // CurrentAverageBuyPrice == The average buy price since the last time the item amount was 0
    [property: JsonRequired, JsonPropertyName("average_buy_price")]
    long AverageBuyPrice,
    [property: JsonRequired, JsonPropertyName("steam_sell_price")]
    long? SteamSellPrice,
    [property: JsonRequired, JsonPropertyName("buff163_sell_price")]
    long? Buff163SellPrice,
    [property: JsonRequired, JsonPropertyName("sales_value")]
    long SalesValue,
    [property: JsonRequired, JsonPropertyName("profit")]
    long Profit,
    [property: JsonRequired, JsonPropertyName("actions")]
    List<ListItemActionResponse> Actions
);

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
    DateTime CreatedUtc);