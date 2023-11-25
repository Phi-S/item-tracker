using infrastructure.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace infrastructure.Database.Repos;

public class ItemListValueRepo(XDbContext dbContext)
{
    public async Task<List<ItemListValueDbModel>> GetAll(ItemListDbModel listDbModel)
    {
        return await dbContext.ItemListValues.Where(value => value.ItemListDbModel.Id == listDbModel.Id).ToListAsync();
    }

    public async Task<ItemListValueDbModel> Add(ItemListValueDbModel itemListValueDbModel)
    {
        return (await dbContext.ItemListValues.AddAsync(itemListValueDbModel)).Entity;
    }
}