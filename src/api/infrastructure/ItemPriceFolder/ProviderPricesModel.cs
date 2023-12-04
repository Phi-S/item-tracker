namespace infrastructure.ItemPriceFolder;

public record ProviderPricesModel(
    DateTime LastModified,
    List<(string itemName, decimal? price)> Prices
);