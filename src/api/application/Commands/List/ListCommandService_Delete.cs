using ErrorOr;

namespace application.Commands.List;

public partial class ListCommandService
{
    public async Task<ErrorOr<Deleted>> DeleteList(string? userId, string listUrl)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var list = await _unitOfWork.ItemListRepo.GetListByUrl(listUrl);
        if (list.IsError)
        {
            return list.FirstError;
        }

        if (list.Value.UserId.Equals(userId) == false)
        {
            return Error.Unauthorized(description: "You dont have access to this list");
        }

        await _unitOfWork.ItemListRepo.DeleteList(list.Value.Id);
        _listResponseCacheService.DeleteCache(listUrl);
        await _unitOfWork.Save();
        return Result.Deleted;
    }
}