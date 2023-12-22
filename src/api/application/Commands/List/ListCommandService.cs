using application.Cache;
using infrastructure.Database.Repos;
using infrastructure.Items;
using Microsoft.Extensions.DependencyInjection;

namespace application.Commands.List;

public partial class ListCommandService
{
    private readonly ItemsService _itemsService;
    private readonly UnitOfWork _unitOfWork;
    private readonly ListResponseCacheService _listResponseCacheService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private const int BuySellLimit = 5000;

    public ListCommandService(
        ItemsService itemsService,
        UnitOfWork unitOfWork,
        ListResponseCacheService listResponseCacheService,
        IServiceScopeFactory serviceScopeFactory
        )
    {
        _itemsService = itemsService;
        _unitOfWork = unitOfWork;
        _listResponseCacheService = listResponseCacheService;
        _serviceScopeFactory = serviceScopeFactory;
    }
}