using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public class ItemListItemActionDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required ItemListDbModel List { get; set; }
    [Required] public required long ItemId { get; set; }
    [Required] [StringLength(1)] public required string Action { get; set; }
    [Required] public required long UnitPrice { get; set; }
    [Required] public required int Amount { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}