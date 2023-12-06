using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Database.Models;

public class ItemPriceDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required long ItemId { get; set; }
    public long? SteamPriceCentsUsd { get; set; }
    public long? Buff163PriceCentsUsd { get; set; }
    [Required] public required ItemPriceRefreshDbModel ItemPriceRefresh { get; set; }
}