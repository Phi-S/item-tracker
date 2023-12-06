using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Throw;

namespace infrastructure.Database;

public class XDbContext : DbContext
{
    public DbSet<ItemListDbModel> Lists { get; set; } = null!;
    public DbSet<ItemListItemActionDbModel> ItemActions { get; set; } = null!;
    public DbSet<ItemListSnapshotDbModel> ListSnapshots { get; set; } = null!;
    public DbSet<ItemPriceRefreshDbModel> PricesRefresh { get; set; } = null!;
    public DbSet<ItemPriceDbModel> Prices { get; set; } = null!;

    private const string DatabaseConnectionStringConfigName = "DatabaseConnectionString";
    private readonly string _databaseConnectionString;

    public XDbContext(IConfiguration configuration)
    {
        var databaseConnectionString = configuration.GetValue<string>(DatabaseConnectionStringConfigName);
        databaseConnectionString.ThrowIfNull().IfEmpty().IfWhiteSpace();
        _databaseConnectionString = databaseConnectionString;
    }

    /// <summary>
    /// DONT USE THIS CONSTRUCTOR
    /// <remarks>
    /// This constructor is only used for the "<c>dotnet ef migration add</c>" command and will not work for anything else
    /// </remarks> 
    /// </summary>
    public XDbContext()
    {
        _databaseConnectionString = "EmptyOnlyForMigration";
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_databaseConnectionString);
        /*.LogTo(s => System.Diagnostics.Debug.WriteLine(s))
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging();*/
        base.OnConfiguring(optionsBuilder);
    }
}