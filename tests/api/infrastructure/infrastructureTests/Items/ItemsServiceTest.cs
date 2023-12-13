using infrastructure.Items;
using Xunit.Abstractions;

namespace infrastructureTests.Items;

public class ItemsServiceTest
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly ItemsService _itemsService = new();

    public ItemsServiceTest(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void EveryItemUniqueName()
    {
        var allItems = _itemsService.GetAll().Value;
        var nonDistinctItems = allItems.GroupBy(item => item.Name).Where(group => group.Count() > 1).ToList();
        if (nonDistinctItems.Count != 0)
        {
            foreach (var nonDistinctItem in nonDistinctItems)
            {
                _outputHelper.WriteLine(
                    nonDistinctItem.Key + ": " + string.Join(", ", nonDistinctItem.Select(item => $"{item.Id}")));
            }
        }

        Assert.Empty(nonDistinctItems);
    }

    [Fact]
    public void GetAllTest()
    {
        var result = _itemsService.GetAll();
        Assert.False(result.IsError);
        Assert.True(result.Value.Count > 0);
    }

    [Fact]
    public void GetByIdTest()
    {
        var allItems = _itemsService.GetAll();
        Assert.False(allItems.IsError);
        var randomSkip = Random.Shared.Next(allItems.Value.Count);
        var randomItem = _itemsService.GetAll().Value.Skip(randomSkip).First();
        var result = _itemsService.GetById(randomItem.Id);
        Assert.False(result.IsError);
        Assert.Equal(randomItem, result.Value);
        Assert.Equal(randomItem.Id, result.Value.Id);
    }

    [Fact]
    public void TestSearch_DirectMatch()
    {
        var searchString = "1st Lieutenant Farlow | SWAT";
        var result = _itemsService.Search(searchString);
        Assert.False(result.IsError);
        Assert.True(result.Value.Count > 1);
        Assert.Equal(searchString, result.Value.First().Name);
    }

    [Fact]
    public void TestSearch_MultipleMatches()
    {
        var searchString = "sticker capsule paris 2023";
        var result = _itemsService.Search(searchString);
        Assert.False(result.IsError);
        _outputHelper.WriteLine(string.Join("\n", result.Value.Select(value => value.Name)));

        var parisCapsules = result.Value.Take(3).ToList();
        foreach (var parisCapsule in parisCapsules)
        {
            Assert.StartsWith("Paris 2023", parisCapsule.Name);
            Assert.EndsWith("Sticker Capsule", parisCapsule.Name);
        }
    }
}