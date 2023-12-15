using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public class ItemListSnapshotDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required ItemListDbModel List { get; set; }
    [Required] public required ItemPriceRefreshDbModel ItemPriceRefresh { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}