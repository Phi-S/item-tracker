using ErrorOr;
using infrastructure.Database.Repos;
using MediatR;
using shared.Currencies;

namespace application.Commands.List;

public record CreateNewListCommand(
    string? UserId,
    string ListName,
    string? ListDescription,
    string Currency,
    bool Public
) : IRequest<ErrorOr<string>>;

public class CreateNewListCommandHandler : IRequestHandler<CreateNewListCommand, ErrorOr<string>>
{
    private readonly UnitOfWork _unitOfWork;

    public CreateNewListCommandHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<string>> Handle(CreateNewListCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Error.Unauthorized(description: "UserId not found");
        }

        var isCurrencyValid =
            CurrenciesConstants.ValidCurrencies.Any(currency => currency.Equals(request.Currency));
        if (isCurrencyValid == false)
        {
            return Error.Failure(description: $"Currency \"{request.Currency}\" is not a valid currency");
        }

        var existingListWithName =
            await _unitOfWork.ItemListRepo.ListNameTakenForUser(request.UserId, request.ListName);
        if (existingListWithName)
        {
            return Error.Conflict(description: $"List with the name \"{request.ListName}\" already exist");
        }

        var url = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        // Replace URL unfriendly characters
        url = url
            .Replace("=", "")
            .Replace("/", "_")
            .Replace("+", "-");

        await _unitOfWork.ItemListRepo.CreateNewList(
            request.UserId,
            url,
            request.ListName,
            request.ListDescription,
            request.Currency,
            request.Public
        );

        await _unitOfWork.Save();
        return url;
    }
}