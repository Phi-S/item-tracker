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

        var listValue = await dbContext.ItemListValues.AddAsync(new ItemListValueDbModel
        {
            Id = 1,
            List = list.Entity,
            SteamValue = 1,
            BuffValue = 1,
            InvestedCapital = 0,
            ItemPriceRefresh = null,
            CreatedUtc = default
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

        var listValue = await dbContext.ItemListValues.AddAsync(new ItemListValueDbModel
        {
            Id = 1,
            List = list.Entity,
            SteamValue = 1,
            BuffValue = 1,
            InvestedCapital = 0,
            ItemPriceRefresh = null,
            CreatedUtc = default
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
        const string listName = "test_list_url";
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
        const string listName = "test_list_url";
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
        const string listName = "test_list_url";
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
}