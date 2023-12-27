using application.Commands.List;
using application.Queries;
using ErrorOr;
using MediatR;
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
            IMediator mediator) =>
        {
            var userId = context.User.Id();
            var getAllListsForUserQuery = new GetAllListsForUserQuery(userId);
            var listResponses = await mediator.Send(getAllListsForUserQuery);
            if (listResponses.IsError)
            {
                return listResponses.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(listResponses.FirstError.Description)
                    : Results.Extensions.InternalServerError(listResponses.FirstError.Description);
            }

            return Results.Ok(listResponses.Value);
        });

        group.MapPost("/new", async (
            HttpContext context,
            IMediator mediator,
            [FromBody] NewListModel newListModel) =>
        {
            var userId = context.User.Id();
            var createNewListCommand = new CreateNewListCommand(userId, newListModel.ListName,
                newListModel.ListDescription, newListModel.Currency, newListModel.Public);
            var newListUrl = await mediator.Send(createNewListCommand);
            if (newListUrl.IsError)
            {
                return newListUrl.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(newListUrl.FirstError.Description)
                    : Results.Extensions.InternalServerError(newListUrl.FirstError.Description);
            }

            return Results.Text(newListUrl.Value);
        });

        group.MapDelete("/delete-action", async (
            HttpContext context,
            IMediator mediator,
            [FromQuery] long actionId) =>
        {
            var userId = context.User.Id();
            var deleteItemActionCommand = new DeleteItemActionCommand(userId, actionId);
            var result = await mediator.Send(deleteItemActionCommand);
            if (result.IsError)
            {
                return result.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(result.FirstError.Description)
                    : Results.Extensions.InternalServerError(result.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapGet("{url}", async (
            HttpContext context,
            IMediator mediator,
            string url) =>
        {
            var userId = context.User.Id();
            var query = new GetListQuery(userId, url);
            var listResponse = await mediator.Send(query);
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
            IMediator mediator,
            string url) =>
        {
            var userId = context.User.Id();
            var deleteListCommand = new DeleteListCommand(userId, url);
            var result = await mediator.Send(deleteListCommand);
            if (result.IsError)
            {
                return result.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(result.FirstError.Description)
                    : Results.Extensions.InternalServerError(result.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPost("{url}/buy-item", async (
            HttpContext context,
            IMediator mediator,
            string url,
            [FromQuery] long itemId,
            [FromQuery] long unitPrice,
            [FromQuery] int amount) =>
        {
            var userId = context.User.Id();
            var addItemActionBuyCommand = new AddItemActionBuyCommand(userId, url, itemId, unitPrice, amount);
            var result = await mediator.Send(addItemActionBuyCommand);
            if (result.IsError)
            {
                return result.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(result.FirstError.Description)
                    : Results.Extensions.InternalServerError(result.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPost("{url}/sell-item", async (
            HttpContext context,
            IMediator mediator,
            string url,
            [FromQuery] long itemId,
            [FromQuery] long unitPrice,
            [FromQuery] int amount) =>
        {
            var userId = context.User.Id();
            var addItemActionSellCommand = new AddItemActionSellCommand(userId, url, itemId, unitPrice, amount);
            var result = await mediator.Send(addItemActionSellCommand);
            if (result.IsError)
            {
                return result.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(result.FirstError.Description)
                    : Results.Extensions.InternalServerError(result.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPut("{url}/update-name", async (
            HttpContext context,
            IMediator mediator,
            string url,
            [FromQuery] string newName) =>
        {
            var userId = context.User.Id();
            var updateListNameCommand = new UpdateListNameCommand(userId, url, newName);
            var result = await mediator.Send(updateListNameCommand);
            if (result.IsError)
            {
                return result.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(result.FirstError.Description)
                    : Results.Extensions.InternalServerError(result.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPut("{url}/update-description", async (
            HttpContext context,
            IMediator mediator,
            string url,
            [FromQuery] string newDescription) =>
        {
            var userId = context.User.Id();
            var updateListDescriptionCommand = new UpdateListDescriptionCommand(userId, url, newDescription);
            var result = await mediator.Send(updateListDescriptionCommand);
            if (result.IsError)
            {
                return result.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(result.FirstError.Description)
                    : Results.Extensions.InternalServerError(result.FirstError.Description);
            }

            return Results.Ok();
        });

        group.MapPut("{url}/update-public", async (
            HttpContext context,
            IMediator mediator,
            string url,
            [FromQuery] bool newPublic) =>
        {
            var userId = context.User.Id();
            var updateListPublicCommand = new UpdateListPublicCommand(userId, url, newPublic);
            var result = await mediator.Send(updateListPublicCommand);
            if (result.IsError)
            {
                return result.FirstError.Type == ErrorType.Unauthorized
                    ? Results.Extensions.Unauthorized(result.FirstError.Description)
                    : Results.Extensions.InternalServerError(result.FirstError.Description);
            }

            return Results.Ok();
        });
    }
}