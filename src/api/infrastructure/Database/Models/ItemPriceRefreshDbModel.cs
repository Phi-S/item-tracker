using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public static class ItemPriceRefreshDbSchema
{
    public static string Id { get; } = nameof(ItemPriceRefreshDbModel.Id).ToSnakeCase();

    public static string UsdToEurExchangeRate { get; } =
        nameof(ItemPriceRefreshDbModel.UsdToEurExchangeRate).ToSnakeCase();

    public static string SteamPricesLastModified { get; } =
        nameof(ItemPriceRefreshDbModel.SteamPricesLastModifiedAt).ToSnakeCase();

    public static string CreatedUtc { get; } = nameof(ItemPriceRefreshDbModel.CreatedAt).ToSnakeCase();
}

public class ItemPriceRefreshDbModel
{
    public required long Id { get; set; }
    public required double UsdToEurExchangeRate { get; set; }
    public required long SteamPricesLastModifiedAt { get; set; }
    public required long CreatedAt { get; set; }
}