using application.Commands;
using Microsoft.AspNetCore.Mvc;

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

        group.MapPost("search", (
            ItemCommandService itemCommandService,
            [FromQuery] string searchString) =>
        {
            var searchResult = itemCommandService.Search(searchString);
            return searchResult.IsError
                ? Results.Extensions.InternalServerError(searchResult.FirstError.Description)
                : Results.Ok(searchResult.Value);
        });
    }
}