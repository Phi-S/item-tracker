using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public class ItemPriceDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required long ItemId { get; set; }
    public decimal? SteamPriceUsd { get; set; }
    public decimal? SteamPriceEur { get; set; }
    public decimal? BuffPriceUsd { get; set; }
    public decimal? BuffPriceEur { get; set; }
    [Required] public required ItemPriceRefreshDbModel ItemPriceRefresh { get; set; }
}