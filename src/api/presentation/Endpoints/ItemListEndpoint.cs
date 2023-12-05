using application.Commands;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using presentation.Extension;
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
            if (lists.IsError)
            {
                return lists.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(lists.FirstError.Description)
                    : Results.Extensions.InternalServerError(lists.FirstError.Description);
            }

            return Results.Ok(lists.Value);
        });

        group.MapPost("/new", async (
            HttpContext context,
            ListCommandService listCommandService,
            [FromBody] NewListModel newListModel) =>
        {
            var userId = context.User.Id();
            var newList = await listCommandService.New(userId, newListModel);
            if (newList.IsError)
            {
                return newList.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(newList.FirstError.Description)
                    : Results.Extensions.InternalServerError(newList.FirstError.Description);
            }

            return Results.Text(newList.Value.Url);
        });

        group.MapGet("{url}", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url) =>
        {
            var userId = context.User.Id();
            var listResponse = await listCommandService.GetList(userId, url);
            if (listResponse.IsError)
            {
                return listResponse.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(listResponse.FirstError.Description)
                    : Results.Extensions.InternalServerError(listResponse.FirstError.Description);
            }

            return Results.Ok(listResponse.Value);
        }).AllowAnonymous();

        group.MapDelete("{url}/delete", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url) =>
        {
            var userId = context.User.Id();
            var buyItem = await listCommandService.DeleteList(userId, url);
            if (buyItem.IsError)
            {
                return buyItem.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(buyItem.FirstError.Description)
                    : Results.Extensions.InternalServerError(buyItem.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPost("{url}/buy-item", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url,
            [FromQuery] long itemId,
            [FromQuery] decimal price,
            [FromQuery] int amount) =>
        {
            var userId = context.User.Id();
            var buyItem = await listCommandService.BuyItem(userId, url, itemId, price, amount);
            if (buyItem.IsError)
            {
                return buyItem.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(buyItem.FirstError.Description)
                    : Results.Extensions.InternalServerError(buyItem.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPost("{url}/sell-item", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url,
            [FromQuery] long itemId,
            [FromQuery] decimal price,
            [FromQuery] int amount) =>
        {
            var userId = context.User.Id();
            var sellItem = await listCommandService.SellItem(userId, url, itemId, price, amount);
            if (sellItem.IsError)
            {
                return sellItem.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(sellItem.FirstError.Description)
                    : Results.Extensions.InternalServerError(sellItem.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPut("{url}/update-name", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url,
            [FromQuery] string newName) =>
        {
            var userId = context.User.Id();
            var updateResult = await listCommandService.UpdateListName(userId, url, newName);
            if (updateResult.IsError)
            {
                return updateResult.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(updateResult.FirstError.Description)
                    : Results.Extensions.InternalServerError(updateResult.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPut("{url}/update-description", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url,
            [FromQuery] string newDescription) =>
        {
            var userId = context.User.Id();
            var updateResult = await listCommandService.UpdateListDescription(userId, url, newDescription);
            if (updateResult.IsError)
            {
                return updateResult.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(updateResult.FirstError.Description)
                    : Results.Extensions.InternalServerError(updateResult.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPut("{url}/update-public", async (
            HttpContext context,
            ListCommandService listCommandService,
            string url,
            [FromQuery] bool newPublic) =>
        {
            var userId = context.User.Id();
            var updateResult = await listCommandService.UpdateListPublic(userId, url, newPublic);
            if (updateResult.IsError)
            {
                return updateResult.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(updateResult.FirstError.Description)
                    : Results.Extensions.InternalServerError(updateResult.FirstError.Description);
            }

            return Results.Ok();
        });
    }
}