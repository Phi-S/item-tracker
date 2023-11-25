namespace shared.Models;

public record ListItemResponse(
    long Id,
    long ItemId,
    string ItemImage,
    string Action,
    decimal PricePerOne,
    long Amount,
    DateTime CreatedUtc);