using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Database.Models;

[Index(nameof(ItemName))]
public class ItemListItemActionDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required ItemListDbModel List { get; set; }
    [Required] [MaxLength(256)] public required string ItemName { get; set; }
    [Required] [StringLength(1)] public required string Action { get; set; }
    [Required] public required long UnitPrice { get; set; }
    [Required] public required int Amount { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}