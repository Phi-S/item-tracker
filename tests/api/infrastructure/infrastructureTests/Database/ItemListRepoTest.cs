using infrastructure.Database;
using infrastructure.Database.Models;
using infrastructure.Database.Repos;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace infrastructureTests.Database;

public class ItemListRepoTest
{
    private readonly ITestOutputHelper _outputHelper;

    public ItemListRepoTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task GetListInfosTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        const long listId = 5;
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = listId,
            UserId = "test_user",
            Name = "test_list",
            Url = "test_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });

        var priceRefresh = await dbContext.PricesRefresh.AddAsync(new ItemPriceRefreshDbModel
        {
            Id = 1,
            SteamPricesLastModified = default,
            Buff163PricesLastModified = default,
            CreatedUtc = default,
            UsdToEurExchangeRate = 1
        });

        var price = await dbContext.AddAsync(new ItemPriceDbModel
        {
            Id = 1,
            ItemId = 1,
            SteamPriceCentsUsd = null,
            Buff163PriceCentsUsd = null,
            ItemPriceRefresh = priceRefresh.Entity
        });

        var snapshot = await dbContext.ListSnapshots.AddAsync(new ItemListSnapshotDbModel
        {
            Id = 1,
            List = list.Entity,
            SteamValue = 1,
            BuffValue = 1,
            ItemPriceRefresh = priceRefresh.Entity,
            CreatedUtc = default
        });

        var itemAction = await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            Id = 1,
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            UnitPrice = 1,
            Amount = 1,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;

        var listInfo = await itemListRepo.GetListInfos(listId);
        Assert.True(listInfo.List.Id == list.Entity.Id);
        Assert.Equal(listId, listInfo.Item1.Id);

        Assert.Single(listInfo.Snapshots);
        Assert.Equal(snapshot.Entity.Id, listInfo.Snapshots.First().Id);

        Assert.Single(listInfo.ItemActions);
        Assert.Equal(itemAction.Entity.Id, listInfo.ItemActions.First().Id);

        Assert.Equal(priceRefresh.Entity.Id, listInfo.LastPriceRefresh.Id);

        Assert.Single(listInfo.PricesForItemsInList);
        Assert.Equal(price.Entity.Id, listInfo.PricesForItemsInList.First().Id);
    }

    [Fact]
    public async Task ExistsWithNameForUserTest_Found()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        const string userId = "test_user_id";
        const string listName = "test_list_name";
        await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = userId,
            Name = listName,
            Url = "test_list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;
        var existsWithNameForUser = await itemListRepo.ExistsWithNameForUser(userId, listName);
        Assert.True(existsWithNameForUser);
    }

    [Fact]
    public async Task ExistsWithNameForUserTest_UserIdNotFound()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        const string userId = "test_user_id";
        const string listName = "test_list_name";
        await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = userId,
            Name = listName,
            Url = "test_list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;
        var existsWithNameForUser = await itemListRepo.ExistsWithNameForUser(userId + "_not_found", listName);
        Assert.True(existsWithNameForUser == false);
    }

    [Fact]
    public async Task ExistsWithNameForUserTest_ListNameNotFound()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        const string userId = "test_user_id";
        const string listName = "test_list_name";
        await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = userId,
            Name = listName,
            Url = "test_list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;
        var existsWithNameForUser = await itemListRepo.ExistsWithNameForUser(userId, listName + "_not_found");
        Assert.True(existsWithNameForUser == false);
    }

    [Fact]
    public async Task GetByUrlTest_Found()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        const string listUrl = "test_list_url";
        const bool deleted = false;
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "test_user_id",
            Name = "test_list_name",
            Url = listUrl,
            Currency = "EUR",
            Public = false,
            Deleted = deleted,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;
        var listResult = await itemListRepo.GetByUrl(listUrl);
        Assert.True(listResult.IsError == false);
        Assert.Equal(listUrl, listResult.Value.Url);
        Assert.Equal(list.Entity.Id, listResult.Value.Id);
    }

    [Fact]
    public async Task GetByUrlTest_Error_Deleted()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        const string listUrl = "test_list_url";
        const bool deleted = true;
        await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "test_user_id",
            Name = "test_list_name",
            Url = listUrl,
            Currency = "EUR",
            Public = false,
            Deleted = deleted,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;
        var listResult = await itemListRepo.GetByUrl(listUrl);
        Assert.True(listResult.IsError);
    }

    [Fact]
    public async Task GetByUrlTest_Error_ListUrlNotFound()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        const string listUrl = "test_list_url";
        const bool deleted = false;
        await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "test_user_id",
            Name = "test_list_name",
            Url = listUrl,
            Currency = "EUR",
            Public = false,
            Deleted = deleted,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;
        var listResult = await itemListRepo.GetByUrl(listUrl + "_not_found");
        Assert.True(listResult.IsError);
    }

    [Fact]
    public async Task CreateNewListTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;

        const string userId = "test_list_user_id";
        const string listName = "test_list_name";
        const string listDescription = "test_list_description";
        const string currency = "test_list_currency";
        const bool makeListPublic = false;
        var list = await itemListRepo.CreateNewList(userId, listName, listDescription, currency, makeListPublic);
        await unitOfWork.Save();

        var dbContext = provider.GetRequiredService<XDbContext>();
        var allLists = dbContext.Lists.ToList();
        Assert.True(allLists.Count == 1);
        Assert.Equal(userId, list.UserId);
        Assert.Equal(listName, list.Name);
        Assert.Equal(listDescription, list.Description);
        Assert.Equal(currency, list.Currency);
        Assert.Equal(makeListPublic, list.Public);
        var listInDb = allLists.First();
        Assert.True(allLists.Count == 1);
        Assert.Equal(userId, listInDb.UserId);
        Assert.Equal(listName, listInDb.Name);
        Assert.Equal(listDescription, listInDb.Description);
        Assert.Equal(currency, listInDb.Currency);
        Assert.Equal(makeListPublic, listInDb.Public);
    }

    [Fact]
    public async Task DeleteListTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        const long listId = 111;
        await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = listId,
            UserId = "test_user_id",
            Name = "test_list_name",
            Url = "test_list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        Assert.True(dbContext.Lists.Count() == 1);
        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;
        await itemListRepo.DeleteList(listId);
        await unitOfWork.Save();
        Assert.True(dbContext.Lists.Any(list => list.Deleted == false) == false);
    }
}