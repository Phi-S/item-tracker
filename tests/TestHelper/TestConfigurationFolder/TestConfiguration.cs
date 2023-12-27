using System.Reflection;
using Microsoft.Extensions.Configuration;
using Throw;

namespace TestHelper.TestConfigurationFolder;

public static class TestConfiguration
{
    public static IConfiguration GetApiAppSettingsTest()
    {
        var currentDirectory = Assembly.GetExecutingAssembly().Location;
        currentDirectory = Path.GetDirectoryName(currentDirectory);
        currentDirectory.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var appSettingsJsonPath = Path.Combine(currentDirectory, "api.appsettings.Test.json");
        var builder = new ConfigurationBuilder()
            .AddJsonFile(appSettingsJsonPath, optional: false)
            .AddEnvironmentVariables();
        return builder.Build();
    }

    public static IConfiguration GetApiAppSettingsTest(string key, string? value)
    {
        return GetApiAppSettingsTest([new KeyValuePair<string, string?>(key, value)]);
    }

    public static IConfiguration GetApiAppSettingsTest(List<KeyValuePair<string, string?>> memorySettings)
    {
        var currentDirectory = Assembly.GetExecutingAssembly().Location;
        currentDirectory = Path.GetDirectoryName(currentDirectory);
        currentDirectory.ThrowIfNull().IfEmpty().IfWhiteSpace();
        var appSettingsJsonPath = Path.Combine(currentDirectory, "api.appsettings.Test.json");
        var builder = new ConfigurationBuilder()
            .AddJsonFile(appSettingsJsonPath, optional: false)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(memorySettings);
        return builder.Build();
    }
}