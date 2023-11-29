using System.Web;
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
            var decodedSearchString = HttpUtility.UrlDecode(searchString);
            var searchResult = itemCommandService.Search(decodedSearchString);
            return searchResult.IsError
                ? Results.Extensions.InternalServerError(searchResult.FirstError.Description)
                : Results.Ok(searchResult.Value);
        });

        group.MapPost("refresh-prices", async (
            PriceCommandService priceCommandService) =>
        {
            var prices = await priceCommandService.RefreshItemPrices();
            return prices.IsError
                ? Results.Extensions.InternalServerError(prices.FirstError.Description)
                : Results.Ok();
        });
    }
}