using System.ComponentModel.DataAnnotations;

namespace infrastructure.Database.Models;

public class ItemListValueDbModel
{
    [Key] public long Id { get; set; }
    [Required] public required ItemListDbModel List { get; set; }
    public decimal? SteamValue { get; set; }
    public decimal? BuffValue { get; set; }
    public ItemPriceRefreshDbModel? ItemPriceRefresh { get; set; }
    [Required] public required DateTime CreatedUtc { get; set; }
}