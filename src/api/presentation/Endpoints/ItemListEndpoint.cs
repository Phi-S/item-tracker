using application.Commands;
using ErrorOr;
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
            .WithTags(tag)
            .RequireAuthorization();

        group.MapGet("/all", async (
            HttpContext context,
            ListCommandService listCommandService) =>
        {
            var userId = context.User.Id();
            var lists = await listCommandService.GetAllForUser(userId);
            return lists.IsError
                ? Results.Extensions.InternalServerError(lists.FirstError.Description)
                : Results.Ok(lists.Value);
        });

        group.MapPost("/new", async (
            HttpContext context,
            ListCommandService listCommandService,
            [FromBody] NewListModel newListModel) =>
        {
            var userId = context.User.Id();
            var newList = await listCommandService.New(userId, newListModel);
            return newList.IsError
                ? Results.Extensions.InternalServerError(newList.FirstError.Description)
                : Results.Text(newList.Value.Url);
        });

        group.MapDelete("{url}/delete", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url) =>
        {
            var userId = context.User.Id();
            var delete = await listCommandService.Delete(userId, url);
            return delete.IsError
                ? Results.Extensions.InternalServerError(delete.FirstError.Description)
                : Results.Ok();
        });

        group.MapGet("{url}", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url) =>
        {
            var userId = context.User.Id();
            var listResponse = await listCommandService.Get(userId, url);
            if (listResponse.IsError)
            {
                return listResponse.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(listResponse.FirstError.Description)
                    : Results.Extensions.InternalServerError(listResponse.FirstError.Description);
            }

            return Results.Ok(listResponse.Value);
        }).AllowAnonymous();

        group.MapPost("{url}/buy-item", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url,
            [FromQuery] long itemId,
            [FromQuery] decimal price,
            [FromQuery] long amount) =>
        {
            var userId = context.User.Id();
            var buyItem = await listCommandService.BuyItem(userId, url, itemId, price, amount);
            return buyItem.IsError
                ? Results.Extensions.InternalServerError(buyItem.FirstError.Description)
                : Results.Ok();
        });

        group.MapPost("{url}/sell-item", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url,
            [FromQuery] long itemId,
            [FromQuery] decimal price,
            [FromQuery] long amount) =>
        {
            var userId = context.User.Id();
            var sellItem = await listCommandService.SellItem(userId, url, itemId, price, amount);
            return sellItem.IsError
                ? Results.Extensions.InternalServerError(sellItem.FirstError.Description)
                : Results.Ok();
        });
    }
}