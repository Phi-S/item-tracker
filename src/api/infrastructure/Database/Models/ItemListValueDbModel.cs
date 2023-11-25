using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public class ItemListValueDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required ItemListDbModel ItemListDbModel { get; set; }
    [Required] public required decimal Value { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}