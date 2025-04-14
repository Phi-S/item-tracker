namespace infrastructure.Database.Models;

public static class ListDbSchema
{
    public static string Id { get; } = nameof(ListDbModel.Id).ToSnakeCase();
    public static string UserId { get; } = nameof(ListDbModel.UserId).ToSnakeCase();
    public static string Name { get; } = nameof(ListDbModel.Name).ToSnakeCase();
    public static string Description { get; } = nameof(ListDbModel.Description).ToSnakeCase();
    public static string Url { get; } = nameof(ListDbModel.Url).ToSnakeCase();
    public static string Currency { get; } = nameof(ListDbModel.Currency).ToSnakeCase();
    public static string Public { get; } = nameof(ListDbModel.Public).ToSnakeCase();
    public static string Deleted { get; } = nameof(ListDbModel.Deleted).ToSnakeCase();
    public static string UpdatedUtc { get; } = nameof(ListDbModel.UpdatedAt).ToSnakeCase();
    public static string CreatedUtc { get; } = nameof(ListDbModel.CreatedAt).ToSnakeCase();
}

public class ListDbModel
{
    public required long Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Url { get; set; }
    public required string Currency { get; set; }
    public required bool Public { get; set; }
    public required bool Deleted { get; set; }
    public required long UpdatedAt { get; set; }
    public required long CreatedAt { get; set; }
}