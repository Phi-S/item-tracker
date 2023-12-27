using System.Web;
using application.Commands.Items;
using application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using presentation.Extension;

namespace presentation.Endpoints;

public static class ItemsEndpoint
{
    public static void MapItemsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        const string tag = "items";

        var group = endpoints.MapGroup(tag)
            .WithOpenApi()
            .RequireAuthorization()
            .WithTags(tag);

        group.MapPost("search", async (
            IMediator mediator,
            [FromQuery] string searchString) =>
        {
            var decodedSearchString = HttpUtility.UrlDecode(searchString);
            var searchItemQuery = new SearchItemQuery(decodedSearchString);
            var result = await mediator.Send(searchItemQuery);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.FirstError.Description)
                : Results.Ok(result.Value);
        });

        group.MapPost("refresh-prices", async (IMediator mediator) =>
        {
            var refreshItemPricesCommand = new RefreshItemPricesCommand();
            var result = await mediator.Send(refreshItemPricesCommand);
            return result.IsError
                ? Results.Extensions.InternalServerError(result.FirstError.Description)
                : Results.Ok();
        });
    }
}