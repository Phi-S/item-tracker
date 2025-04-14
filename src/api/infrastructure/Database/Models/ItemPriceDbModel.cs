using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Database.Models;

[Index(nameof(ItemName))]
public class ItemPriceDbModel
{
    [Key] public long Id { get; set; }
    [Required] [MaxLength(256)] public required string ItemName { get; set; }
    public long? SteamPriceCentsUsd { get; set; }
    [Required] public required ItemPriceRefreshDbModel ItemPriceRefresh { get; set; }
}