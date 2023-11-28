using System.Text.Json.Serialization;

namespace shared.Models;

[JsonSerializable(typeof(NewListModel))]
public class NewListModel
{
    public string ListName { get; set; } = "";
    public string? ListDescription { get; set; }
    public string Currency { get; set; } = "";
    public bool Public { get; set; }

    public override string ToString()
    {
        return $"{nameof(ListName)}: {ListName}, {nameof(ListDescription)}: {ListDescription}, {nameof(Currency)}: {Currency}, {nameof(Public)}: {Public}";
    }
}