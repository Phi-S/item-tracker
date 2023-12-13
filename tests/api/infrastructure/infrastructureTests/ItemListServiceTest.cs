using infrastructure.Items;
using Xunit.Abstractions;

namespace infrastructureTests;

public class ItemListServiceTest
{
    private readonly ITestOutputHelper _outputHelper;

    public ItemListServiceTest(ITestOutputHelper outputHelper)
    {
        this._outputHelper = outputHelper;
    }

    [Fact]
    public void GetAllItems()
    {
        var itemListService = new ItemsService();
        var res = itemListService.GetAll();
        if (res.IsError)
        {
            _outputHelper.WriteLine(res.FirstError.ToString());
            Assert.Fail(res.FirstError.Description);
        }

        _outputHelper.WriteLine($"{res.Value.Count} items found");
        Assert.True(true);
    }

    [Fact]
    public void SearchTest()
    {
        var itemListService = new ItemsService();
        var search = itemListService.Search("danger");
        if (search.IsError)
        {
            Assert.Fail(search.FirstError.Description);
        }

        _outputHelper.WriteLine($"search result:\n{string.Join("\n", search.Value.Select(model => model.Name))}");
        Assert.True(search.Value.Count != 0);
    }
}