using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public class ItemPriceRefreshDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}