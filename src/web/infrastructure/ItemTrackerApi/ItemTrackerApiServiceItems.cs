using System.Text.Json;
using System.Web;
using application;
using ErrorOr;
using shared.Models;

namespace infrastructure.ItemTrackerApi;

public partial class ItemTrackerApiService
{
    public async Task<ErrorOr<List<ItemSearchResponse>>> Search(string searchString, string accessToken)
    {
        var encodedSearchString = HttpUtility.UrlEncode(searchString);
        var url = new Uri($"{_apiEndpointUrl}/items/search");
        url = url.AddParameter("searchString", encodedSearchString);

        var response = await PostWithAuthAsync(url.AbsoluteUri, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        var result = JsonSerializer.Deserialize<List<ItemSearchResponse>>(response.Value, SerializerOptions);
        if (result is null)
        {
            return Error.Failure("Failed to deserialize json");
        }

        return result;
    }
}