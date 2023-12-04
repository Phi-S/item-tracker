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
    public async Task GetListInfosForUserIdTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var userId = "test_user";
        var list = await dbContext.ItemLists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = userId,
            Name = "test_list",
            Url = "test_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });

        var priceRefresh = await dbContext.ItemPriceRefresh.AddAsync(new ItemPriceRefreshDbModel
        {
            Id = 1,
            SteamPricesLastModified = default,
            Buff163PricesLastModified = default,
            CreatedUtc = default
        });

        var listValue = await dbContext.ItemListValues.AddAsync(new ItemListSnapshotDbModel
        {
            Id = 1,
            List = list.Entity,
            SteamValue = 1,
            BuffValue = 1,
            ItemPriceRefresh = priceRefresh.Entity,
            CreatedUtc = default,
            InvestedCapital = 0,
            ItemCount = 0
        });

        var itemAction = await dbContext.ItemListItemAction.AddAsync(new ItemListItemActionDbModel
        {
            Id = 1,
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            PricePerOne = 1,
            Amount = 1,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
        var listInfos = await itemListRepo.GetListInfosForUserId(userId);
        Assert.True(listInfos.Count == 1);
        var listInfo = listInfos.First();
        Assert.True(listInfo.Item1.Id == list.Entity.Id);
        Assert.Equal(userId, listInfo.Item1.UserId);

        Assert.True(listInfo.Item2.Count == 1);
        Assert.True(listInfo.Item2.First().Id == listValue.Entity.Id);

        Assert.True(listInfo.Item3.Count == 1);
        Assert.True(listInfo.Item3.First().Id == itemAction.Entity.Id);
    }

    [Fact]
    public async Task GetListInfosTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        long listId = 5;
        var list = await dbContext.ItemLists.AddAsync(new ItemListDbModel
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

        var priceRefresh = await dbContext.ItemPriceRefresh.AddAsync(new ItemPriceRefreshDbModel
        {
            Id = 1,
            SteamPricesLastModified = default,
            Buff163PricesLastModified = default,
            CreatedUtc = default
        });

        var listValue = await dbContext.ItemListValues.AddAsync(new ItemListSnapshotDbModel
        {
            Id = 1,
            List = list.Entity,
            SteamValue = 1,
            BuffValue = 1,
            ItemPriceRefresh = priceRefresh.Entity,
            CreatedUtc = default,
            InvestedCapital = 0,
            ItemCount = 0
        });

        var itemAction = await dbContext.ItemListItemAction.AddAsync(new ItemListItemActionDbModel
        {
            Id = 1,
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            PricePerOne = 1,
            Amount = 1,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
        var listInfo = itemListRepo.GetListInfos(listId);
        Assert.True(listInfo.list.Id == list.Entity.Id);
        Assert.Equal(listId, listInfo.Item1.Id);

        Assert.True(listInfo.listValues.Count == 1);
        Assert.True(listInfo.listValues.First().Id == listValue.Entity.Id);

        Assert.True(listInfo.items.Count == 1);
        Assert.True(listInfo.items.First().Id == itemAction.Entity.Id);
    }

    [Fact]
    public async Task ExistsWithNameForUserTest_Found()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        const string userId = "test_user_id";
        const string listName = "test_list_name";
        await dbContext.ItemLists.AddAsync(new ItemListDbModel
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

        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
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
        await dbContext.ItemLists.AddAsync(new ItemListDbModel
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

        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
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
        await dbContext.ItemLists.AddAsync(new ItemListDbModel
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

        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
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
        var list = await dbContext.ItemLists.AddAsync(new ItemListDbModel
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

        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
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
        await dbContext.ItemLists.AddAsync(new ItemListDbModel
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

        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
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
        await dbContext.ItemLists.AddAsync(new ItemListDbModel
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

        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
        var listResult = await itemListRepo.GetByUrl(listUrl + "_not_found");
        Assert.True(listResult.IsError);
    }

    [Fact]
    public async Task CreateNewListTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var itemListRepo = provider.GetRequiredService<ItemListRepo>();

        const string userId = "test_list_user_id";
        const string listName = "test_list_name";
        const string listDescription = "test_list_description";
        const string currency = "test_list_currency";
        const bool makeListPublic = false;
        var list = await itemListRepo.CreateNewList(userId, listName, listDescription, currency, makeListPublic);

        var dbContext = provider.GetRequiredService<XDbContext>();
        var allLists = dbContext.ItemLists.ToList();
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
        await dbContext.ItemLists.AddAsync(new ItemListDbModel
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

        Assert.True(dbContext.ItemLists.Count() == 1);
        var itemListRepo = provider.GetRequiredService<ItemListRepo>();
        await itemListRepo.DeleteList(listId);
        Assert.True(dbContext.ItemLists.Any(list => list.Deleted == false) == false);
    }
}