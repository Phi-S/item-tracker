using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public static class ListActionDbSchema
{
    public static string Id { get; } = nameof(ListActionDbModel.Id).ToSnakeCase();
    public static string ListId { get; } = nameof(ListActionDbModel.ListId).ToSnakeCase();
    public static string ItemName { get; } = nameof(ListActionDbModel.ItemName).ToSnakeCase();
    public static string Action { get; } = nameof(ListActionDbModel.Action).ToSnakeCase();
    public static string UnitPrice { get; } = nameof(ListActionDbModel.UnitPrice).ToSnakeCase();
    public static string Amount { get; } = nameof(ListActionDbModel.Amount).ToSnakeCase();
    public static string CreatedUtc { get; } = nameof(ListActionDbModel.CreatedAt).ToSnakeCase();
}

public class ListActionDbModel
{
    public required long Id { get; set; }
    public required long ListId { get; set; }
    public required string ItemName { get; set; }
    public required string Action { get; set; }
    public required long UnitPrice { get; set; }
    public required int Amount { get; set; }
    public required long CreatedAt { get; set; }
}