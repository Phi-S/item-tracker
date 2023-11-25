using System.Text.Json.Serialization;

namespace shared.Models;

[JsonSerializable(typeof(NewListModel))]
public record NewListModel(string ListName, string? ListDescription, string Currency, bool Public);