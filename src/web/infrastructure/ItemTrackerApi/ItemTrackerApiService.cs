using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Web;
using ErrorOr;
using Microsoft.Extensions.DependencyInjection;
using OneOf.Types;
using shared.Models;
using Throw;
using Error = ErrorOr.Error;

namespace infrastructure.ItemTrackerApi;

public class ItemTrackerApiService([FromKeyedServices(nameof(ItemTrackerApiService))] HttpClient httpClient)
{
    private async Task<ErrorOr<string>> SendWithAuthAsync(HttpRequestMessage requestMessage, string accessToken)
    {
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Headers.Add("accept", "application/json");
        requestMessage.Headers.Add("Access-Control-Request-Headers", "content-type");
        requestMessage.Headers.Add("Access-Control-Allow-Origin", "*");
        var response = await httpClient.SendAsync(requestMessage);
        if (response.IsSuccessStatusCode == false)
        {
            return response.StatusCode == HttpStatusCode.Unauthorized
                ? Error.Unauthorized()
                : Error.Failure($"Request failed with status code {response.StatusCode}");
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return json;
    }

    #region List

    public async Task<ErrorOr<List<ItemSearchResponse>>> Search(string searchString, string accessToken)
    {
        var encodedSearchString = HttpUtility.UrlEncode(searchString);
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{httpClient.BaseAddress}items/search?searchString={encodedSearchString}");
        var response = await SendWithAuthAsync(request, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        var result = JsonSerializer.Deserialize<List<ItemSearchResponse>>(response.Value);
        if (result is null)
        {
            return Error.Failure("Failed to deserialize json");
        }

        return result;
    }

    #endregion
}