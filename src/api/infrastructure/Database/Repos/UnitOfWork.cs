namespace infrastructure.Database.Repos;

public class UnitOfWork
{
    protected readonly XDbContext DbContext;
    public ItemListRepo ItemListRepo { get; }
    public ItemListSnapshotRepo ItemListSnapshotRepo { get; }
    public ItemPriceRepo ItemPriceRepo { get; }
    
    public UnitOfWork(XDbContext dbContext)
    {
        DbContext = dbContext;
        ItemListRepo = new ItemListRepo(dbContext);
        ItemListSnapshotRepo = new ItemListSnapshotRepo(dbContext);
        ItemPriceRepo = new ItemPriceRepo(dbContext);
    }

    public async Task Save()
    {
        await DbContext.SaveChangesAsync();
    }
}