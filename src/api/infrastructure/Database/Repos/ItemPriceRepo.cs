﻿using ErrorOr;
using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using Error = ErrorOr.Error;

namespace infrastructure.Database.Repos;

public class ItemPriceRepo
{
    private readonly XDbContext _dbContext;

    public ItemPriceRepo(XDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ItemPriceRefreshDbModel> CreateNew(
        double usdToEurExchangeRate,
        DateTime steamPricesLastModified,
        DateTime buff163PricesLastModified)
    {
        var newItemPriceRefresh = await _dbContext.PricesRefresh.AddAsync(
            new ItemPriceRefreshDbModel
            {
                UsdToEurExchangeRate = usdToEurExchangeRate,
                SteamPricesLastModified = steamPricesLastModified,
                Buff163PricesLastModified = buff163PricesLastModified,
                CreatedUtc = DateTime.UtcNow
            });
        return newItemPriceRefresh.Entity;
    }

    public Task Add(IEnumerable<ItemPriceDbModel> itemPrices)
    {
        return _dbContext.Prices.AddRangeAsync(itemPrices);
    }

    public async Task<ErrorOr<ItemPriceRefreshDbModel>> GetLatest()
    {
        var latestItemPriceRefresh = await
            _dbContext.PricesRefresh.OrderByDescending(refresh => refresh.CreatedUtc).FirstOrDefaultAsync();
        if (latestItemPriceRefresh is null)
        {
            return Error.NotFound(description: "No price refresh found");
        }

        return latestItemPriceRefresh;
    }

    public Task<List<ItemPriceRefreshDbModel>> GetSince(DateTime since)
    {
        return Task.FromResult(
            _dbContext.PricesRefresh.Where(priceRefresh => priceRefresh.CreatedUtc >= since).ToList());
    }

    public async Task<ErrorOr<ItemPriceDbModel>> GetPriceForItem(long itemId, ItemPriceRefreshDbModel priceRefresh)
    {
        var price = await _dbContext.Prices.FirstOrDefaultAsync(price =>
            price.ItemPriceRefresh.Id == priceRefresh.Id && price.ItemId == itemId);
        if (price is null)
        {
            return Error.NotFound(
                description:
                $"No price found for item with the id \"{itemId}\" and the price refresh: Id: {priceRefresh.Id} CreatedUtc: {priceRefresh.CreatedUtc}");
        }

        return price;
    }
}