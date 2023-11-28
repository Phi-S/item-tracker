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
            .RequireCors("_myAllowSpecificOrigins")
            .WithTags(tag);

        group.MapPost("search", async (
            ILogger<Program> logger,
            HttpContext context,
            ItemCommandService itemCommandService,
            [FromQuery] string searchString) =>
        {
            var userId = context.User.Id();
            if (userId.IsError)
            {
                var errorString = userId.FirstError.Description;
                logger.LogError("UserId error: {Error}", errorString);
                return Results.Extensions.Unauthorized(errorString);
            }

            var searchResult = itemCommandService.Search(searchString);
            if (searchResult.IsError)
            {
                return Results.Extensions.InternalServerError(searchResult.FirstError.Description);
            }

            return Results.Ok(searchResult.Value);
        });
    }
}