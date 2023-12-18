using System.Text.Json.Serialization;

namespace shared.Models.ListResponse;

public record ListItemResponse(
    [property: JsonRequired, JsonPropertyName("item_id")]
    long ItemId,
    [property: JsonRequired, JsonPropertyName("item_name")]
    string ItemName,
    [property: JsonRequired, JsonPropertyName("item_image")]
    string ItemImage,
    [property: JsonRequired, JsonPropertyName("item_count")]
    int ItemCount,
    [property: JsonRequired, JsonPropertyName("invested_capital")]
    long InvestedCapital,
    // CurrentAverageBuyPrice == The average buy price since the last time the item amount was 0
    [property: JsonRequired, JsonPropertyName("average_buy_price_for_one")]
    long AverageBuyPriceForOne,
    [property: JsonRequired, JsonPropertyName("steam_sell_price_for_one")]
    long? SteamSellPriceForOne,
    [property: JsonRequired, JsonPropertyName("buff163_sell_price_for_one")]
    long? Buff163SellPriceForOne,
    [property: JsonRequired, JsonPropertyName("steam_performance_percent")]
    double? SteamPerformancePercent,
    [property: JsonRequired, JsonPropertyName("buff163_performance_percent")]
    double? Buff163PerformancePercent,
    [property: JsonRequired, JsonPropertyName("steam_performance_value")]
    long? SteamPerformanceValue,
    [property: JsonRequired, JsonPropertyName("buff163_performance_value")]
    long? Buff163PerformanceValue,
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