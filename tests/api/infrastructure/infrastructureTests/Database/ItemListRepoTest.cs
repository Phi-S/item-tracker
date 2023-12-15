using infrastructure.Database;
using infrastructure.Database.Models;
using infrastructure.Database.Repos;
using Microsoft.EntityFrameworkCore;
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

    #region List

    [Fact]
    public async Task CreateNewListTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemListRepo = unitOfWork.ItemListRepo;

        const string userId = "test_list_user_id";
        const string listUrl = "test_list_url";
        const string listName = "test_list_name";
        const string listDescription = "test_list_description";
        const string currency = "test_list_currency";
        const bool makeListPublic = false;
        var list = await itemListRepo.CreateNewList(userId, listUrl, listName, listDescription, currency,
            makeListPublic);
        await unitOfWork.Save();

        var dbContext = provider.GetRequiredService<XDbContext>();
        var allLists = dbContext.Lists.ToList();
        Assert.Single(allLists);
        Assert.Equal(userId, list.UserId);
        Assert.Equal(listName, list.Name);
        Assert.Equal(listDescription, list.Description);
        Assert.Equal(currency, list.Currency);
        Assert.Equal(makeListPublic, list.Public);
        var listInDb = allLists.First();
        Assert.Single(allLists);
        Assert.Equal(userId, listInDb.UserId);
        Assert.Equal(listName, listInDb.Name);
        Assert.Equal(listDescription, listInDb.Description);
        Assert.Equal(currency, listInDb.Currency);
        Assert.Equal(makeListPublic, listInDb.Public);
    }

    [Fact]
    public async Task UpdateListNameTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "list_name",
            Description = null,
            Url = "list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        const string updatedName = "updated_list_name";
        await unitOfWork.ItemListRepo.UpdateListName(list.Entity.Id, updatedName);
        await unitOfWork.Save();
        var updatedList = await dbContext.Lists.FindAsync(list.Entity.Id);
        Assert.NotNull(updatedList);
        Assert.Equal(updatedName, updatedList.Name);
    }

    [Fact]
    public async Task UpdateListDescriptionTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "list_name",
            Description = null,
            Url = "list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        const string updatedDescription = "updated_list_description";
        await unitOfWork.ItemListRepo.UpdateListDescription(list.Entity.Id, updatedDescription);
        await unitOfWork.Save();
        var updatedList = await dbContext.Lists.FindAsync(list.Entity.Id);
        Assert.NotNull(updatedList);
        Assert.Equal(updatedDescription, updatedList.Description);
    }

    [Fact]
    public async Task UpdateListPublicTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "list_name",
            Description = null,
            Url = "list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        await unitOfWork.ItemListRepo.UpdateListPublicState(list.Entity.Id, true);
        await unitOfWork.Save();
        var updatedList = await dbContext.Lists.FindAsync(list.Entity.Id);
        Assert.NotNull(updatedList);
        Assert.True(updatedList.Public);
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
    public async Task GetAllListsForUserTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var userId = "test_user_id";
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = userId,
            Name = "list_name",
            Description = null,
            Url = "list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        var itemList = await unitOfWork.ItemListRepo.GetAllListsForUser(userId);
        Assert.Single(itemList);
        Assert.Equal(list.Entity.Id, itemList.First().Id);
        Assert.Equal(userId, itemList.First().UserId);
    }

    [Fact]
    public async Task ListNameTakenForUserTest_Found()
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
        var existsWithNameForUser = await itemListRepo.ListNameTakenForUser(userId, listName);
        Assert.True(existsWithNameForUser);
    }

    [Fact]
    public async Task ListNameTakenForUserTest_UserIdNotFound()
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
        var existsWithNameForUser = await itemListRepo.ListNameTakenForUser(userId + "_not_found", listName);
        Assert.True(existsWithNameForUser == false);
    }

    [Fact]
    public async Task ListNameTakenForUserTest_ListNameNotFound()
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
        var existsWithNameForUser = await itemListRepo.ListNameTakenForUser(userId, listName + "_not_found");
        Assert.True(existsWithNameForUser == false);
    }

    [Fact]
    public async Task GetListByUrlTest_Found()
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
        var listResult = await itemListRepo.GetListByUrl(listUrl);
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
        var listResult = await itemListRepo.GetListByUrl(listUrl);
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
        var listResult = await itemListRepo.GetListByUrl(listUrl + "_not_found");
        Assert.True(listResult.IsError);
    }

    #endregion


    [Fact]
    public async Task GetListItemCountTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "list_name",
            Description = null,
            Url = "list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });

        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            Id = 1,
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            UnitPrice = 0,
            Amount = 1,
            CreatedUtc = default
        });

        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            Id = 2,
            List = list.Entity,
            ItemId = 1,
            Action = "S",
            UnitPrice = 0,
            Amount = 1,
            CreatedUtc = default
        });

        await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            Id = 3,
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            UnitPrice = 0,
            Amount = 5,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();
        var unitOfWork = provider.GetRequiredService<UnitOfWork>();

        var itemCount = await unitOfWork.ItemListRepo.GetListItemCount(list.Entity.Id, 1);
        Assert.Equal(5, itemCount);
    }

    [Fact]
    public async Task AddItemAction()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "list_name",
            Description = null,
            Url = "list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();
        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        await unitOfWork.ItemListRepo.AddItemAction("B", list.Entity, 1, 1, 1);
        await unitOfWork.Save();
        var actionInList = await dbContext.ItemActions.FirstAsync(action => action.List.Id == list.Entity.Id);
        Assert.Equal("B", actionInList.Action);
        Assert.Equal(1, actionInList.ItemId);
        Assert.Equal(1, actionInList.UnitPrice);
        Assert.Equal(1, actionInList.Amount);
    }

    [Fact]
    public async Task DeleteItemAction()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "list_name",
            Description = null,
            Url = "list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        var action = await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
        {
            Id = 1,
            List = list.Entity,
            ItemId = 1,
            Action = "B",
            UnitPrice = 0,
            Amount = 1,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();
        Assert.Equal(1, dbContext.ItemActions.Count());
        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        await unitOfWork.ItemListRepo.DeleteItemAction(list.Entity, action.Entity.Id);
        await unitOfWork.Save();
        Assert.Equal(0, dbContext.ItemActions.Count());
    }

    [Fact]
    public async Task GetItemActionByIdTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "list_name",
            Description = null,
            Url = "list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });
        var action = await dbContext.ItemActions.AddAsync(new ItemListItemActionDbModel
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
        var actionFromDb = await unitOfWork.ItemListRepo.GetItemActionById(action.Entity.Id);
        Assert.Equal(action.Entity.Id, actionFromDb.Id);
        Assert.Equal(action.Entity.List.Id, actionFromDb.List.Id);
        Assert.Equal(action.Entity.ItemId, actionFromDb.ItemId);
        Assert.Equal(action.Entity.Action, actionFromDb.Action);
        Assert.Equal(action.Entity.UnitPrice, actionFromDb.UnitPrice);
        Assert.Equal(action.Entity.Amount, actionFromDb.Amount);
    }

    [Fact]
    public async Task NewSnapshotTest()
    {
        var serviceCollection = await ServicesSetup.GetApiInfrastructureCollection(_outputHelper);
        await using var provider = serviceCollection.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<XDbContext>();
        var list = await dbContext.Lists.AddAsync(new ItemListDbModel
        {
            Id = 1,
            UserId = "user_id",
            Name = "list_name",
            Description = null,
            Url = "list_url",
            Currency = "EUR",
            Public = false,
            Deleted = false,
            UpdatedUtc = default,
            CreatedUtc = default
        });

        var priceRefresh = await dbContext.PricesRefresh.AddAsync(new ItemPriceRefreshDbModel
        {
            Id = 1,
            UsdToEurExchangeRate = 2,
            SteamPricesLastModified = default,
            Buff163PricesLastModified = default,
            CreatedUtc = default
        });
        await dbContext.SaveChangesAsync();

        Assert.Empty(dbContext.ListSnapshots);
        var unitOfWork = provider.GetRequiredService<UnitOfWork>();
        await unitOfWork.ItemListRepo.NewSnapshot(list.Entity, priceRefresh.Entity);
        await unitOfWork.Save();
        Assert.Single(dbContext.ListSnapshots);
        var snapshotFromDb = dbContext.ListSnapshots.Include(itemListSnapshotDbModel => itemListSnapshotDbModel.List)
            .Include(itemListSnapshotDbModel => itemListSnapshotDbModel.ItemPriceRefresh).First();
        Assert.Equal(list.Entity.Id, snapshotFromDb.List.Id);
        Assert.Equal(priceRefresh.Entity.Id, snapshotFromDb.ItemPriceRefresh.Id);
    }
}