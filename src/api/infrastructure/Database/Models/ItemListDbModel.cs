using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Database.Models;

[Index(nameof(Url), IsUnique = true)]
public class ItemListDbModel
{
    [Key] public long Id { get; set; }
    [Required] [MaxLength(36)] public required string UserId { get; set; }
    [Required] [MaxLength(64)] public required string Name { get; set; }
    [MaxLength(256)] public string? Description { get; set; }
    [Required] [MaxLength(22)] public required string Url { get; set; }
    [Required] public required string Currency { get; set; }
    [Required] public required bool Public { get; set; }
    [Required] public required bool Deleted { get; set; }
    [Required] public required DateTime UpdatedUtc { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}