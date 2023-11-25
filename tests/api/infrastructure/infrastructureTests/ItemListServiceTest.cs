using infrastructure.Items;
using Xunit.Abstractions;

namespace infrastructureTests;

public class ItemListServiceTest(ITestOutputHelper output)
{
    [Fact]
    public void GetAllItems()
    {
        var itemListService = new ItemsService();
        var res = itemListService.GetAll();
        if (res.IsError)
        {
            output.WriteLine(res.FirstError.ToString());
            Assert.Fail(res.FirstError.ToString() ?? string.Empty);
        }

        output.WriteLine($"{res.Value.Count} items found");
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

        output.WriteLine($"search result:\n {string.Join("\n", search.Value.Select(model => model.Name))}");
        Assert.True(search.Value.Count != 0);
    }
}