using application.Commands;
using Microsoft.AspNetCore.Mvc;
using shared.Models;

namespace presentation.Endpoints;

public static class ItemListEndpoint
{
    public static void MapItemListEndpoints(this IEndpointRouteBuilder endpoints)
    {
        const string tag = "list";

        var group = endpoints.MapGroup(tag)
            .WithOpenApi()
            .RequireAuthorization()
            .WithTags(tag);

        group.MapGet("/all", async (
            ILogger<Program> logger,
            HttpContext context,
            ListCommandService listCommandService) =>
        {
            var userId = context.User.Id();
            if (userId.IsError)
            {
                var errorString = userId.FirstError.Description;
                logger.LogError("UserId error: {Error}", errorString);
                return Results.Extensions.Unauthorized(errorString);
            }

            var lists = await listCommandService.GetAllForUser(userId.Value);
            return Results.Ok(lists);
        });

        group.MapPost("/new", async (
            ILogger<Program> logger,
            HttpContext context,
            ListCommandService listCommandService,
            [FromBody] NewListModel newListModel) =>
        {
            var userId = context.User.Id();
            if (userId.IsError)
            {
                var errorString = userId.FirstError.Description;
                logger.LogError("UserId error: {Error}", errorString);
                return Results.Extensions.Unauthorized(errorString);
            }

            var newList = await listCommandService.New(userId.Value, newListModel);
            return newList.IsError
                ? Results.Extensions.InternalServerError(newList.FirstError.Description)
                : Results.Ok(newList);
        });

        group.MapGet("{url}", async (
            ILogger<Program> logger,
            HttpContext context,
            ListCommandService listCommandService,
            string url) =>
        {
            var userId = context.User.Id();
            if (userId.IsError)
            {
                var errorString = userId.FirstError.Description;
                logger.LogError("UserId error: {Error}", errorString);
                return Results.Extensions.Unauthorized(errorString);
            }

            var listResponse = await listCommandService.Get(userId.Value, url);
            return listResponse.IsError
                ? Results.Extensions.InternalServerError(listResponse.FirstError.Description)
                : Results.Ok(listResponse.Value);
        });

        group.MapPost("{url}/delete", async (
            ILogger<Program> logger,
            HttpContext context,
            ListCommandService listCommandService,
            string url) =>
        {
            var userId = context.User.Id();
            if (userId.IsError)
            {
                var errorString = userId.FirstError.Description;
                logger.LogError("UserId error: {Error}", errorString);
                return Results.Extensions.Unauthorized(errorString);
            }

            var delete = await listCommandService.Delete(userId.Value, url);
            return delete.IsError
                ? Results.Extensions.InternalServerError(delete.FirstError.Description)
                : Results.Ok();
        });

        group.MapPost("{url}/buy-item", async (
            ILogger<Program> logger,
            HttpContext context,
            ListCommandService listCommandService,
            string url,
            [FromQuery] long itemId,
            [FromQuery] decimal price,
            [FromQuery] long amount) =>
        {
            var userId = context.User.Id();
            if (userId.IsError)
            {
                var errorString = userId.FirstError.Description;
                logger.LogError("UserId error: {Error}", errorString);
                return Results.Extensions.Unauthorized(errorString);
            }

            var buyItem = await listCommandService.BuyItem(userId.Value, url, itemId, price, amount);
            return buyItem.IsError
                ? Results.Extensions.InternalServerError(buyItem.FirstError.Description)
                : Results.Ok();
        });

        group.MapPost("{url}/sell-item", async (
            ILogger<Program> logger,
            HttpContext context,
            ListCommandService listCommandService,
            string url,
            [FromQuery] long itemId,
            [FromQuery] decimal price,
            [FromQuery] long amount) =>
        {
            var userId = context.User.Id();
            if (userId.IsError)
            {
                var errorString = userId.FirstError.Description;
                logger.LogError("UserId error: {Error}", errorString);
                return Results.Extensions.Unauthorized(errorString);
            }

            var sellItem = await listCommandService.SellItem(userId.Value, url, itemId, price, amount);
            return sellItem.IsError
                ? Results.Extensions.InternalServerError(sellItem.FirstError.Description)
                : Results.Ok();
        });
    }
}