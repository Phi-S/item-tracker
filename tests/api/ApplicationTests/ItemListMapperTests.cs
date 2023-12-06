using System.Diagnostics;
using application.Commands;
using infrastructure.Database;
using infrastructure.Database.Models;
using infrastructure.Items;
using Microsoft.Extensions.DependencyInjection;
using TestHelper.TestSetup;
using Xunit.Abstractions;

namespace ApplicationTests;

public class ItemListMapper
{
    private readonly ITestOutputHelper _outputHelper;

    public ItemListMapper(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task RefreshItemPricesTest()
    {
       
    }
}