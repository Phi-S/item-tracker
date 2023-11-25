namespace shared.Models;

public record ListResponse(
    string Name,
    string? Description,
    string Url,
    string Currency,
    bool Public,
    List<ListItemResponse> Items,
    List<ListValueResponse> ListValues);