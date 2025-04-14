using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public static class ItemPriceDbSchema
{
    public static string Id { get; set; } = nameof(ItemPriceDbModel.Id).ToSnakeCase();
    public static string ItemName { get; set; } = nameof(ItemPriceDbModel.ItemName).ToSnakeCase();
    public static string SteamPriceCentsUsd { get; set; } = nameof(ItemPriceDbModel.SteamPriceCentsUsd).ToSnakeCase();
    public static string ItemPriceRefreshId { get; set; } = nameof(ItemPriceDbModel.ItemPriceRefreshId).ToSnakeCase();
}

public class ItemPriceDbModel
{
    public required long Id { get; set; }
    public required string ItemName { get; set; }
    public long? SteamPriceCentsUsd { get; set; }
    public required long ItemPriceRefreshId { get; set; }
}