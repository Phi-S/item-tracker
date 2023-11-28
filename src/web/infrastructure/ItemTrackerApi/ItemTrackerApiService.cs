using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using ErrorOr;
using Microsoft.Extensions.Configuration;
using shared.Models;
using Throw;

namespace infrastructure.ItemTrackerApi;

public class ItemTrackerApiService
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly string _apiEndpointUrl;
    private readonly HttpClient _httpClient;

    public ItemTrackerApiService(IConfiguration configuration, HttpClient httpClient)
    {
        var apiEndpointUrl = configuration.GetValue<string>("ITEM_TRACKER_API_ENDPOINT");
        apiEndpointUrl.ThrowIfNull().IfEmpty().IfWhiteSpace();

        _apiEndpointUrl = apiEndpointUrl;
        _httpClient = httpClient;
    }

    private async Task<ErrorOr<string>> SendWithAuthAsync(HttpRequestMessage requestMessage, string accessToken)
    {
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Headers.Add("accept", "application/json");
        requestMessage.Headers.Add("Access-Control-Request-Headers", "content-type");
        var response = await _httpClient.SendAsync(requestMessage);
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

    private async Task<ErrorOr<string>> SendAsync(HttpRequestMessage requestMessage)
    {
        requestMessage.Headers.Add("accept", "application/json");
        requestMessage.Headers.Add("Access-Control-Request-Headers", "content-type");
        var response = await _httpClient.SendAsync(requestMessage);
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

    public async Task<ErrorOr<List<ListMiniResponse>>> All(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_apiEndpointUrl}/list/all");
        var response = await SendWithAuthAsync(request, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        var result = JsonSerializer.Deserialize<List<ListMiniResponse>>(response.Value, SerializerOptions);
        if (result is null)
        {
            return Error.Failure("Failed to deserialize json");
        }

        return result;
    }

    public async Task<ErrorOr<ListResponse>> NewList(string accessToken, NewListModel newListModel)
    {
        var newListModelJson = JsonSerializer.Serialize(newListModel);
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_apiEndpointUrl}/list/new");
        request.Content = new StringContent(newListModelJson, Encoding.UTF8, "application/json");
        var response = await SendWithAuthAsync(request, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        var result = JsonSerializer.Deserialize<ListResponse>(response.Value, SerializerOptions);
        if (result is null)
        {
            return Error.Failure("Failed to deserialize json");
        }

        return result;
    }

    public async Task<ErrorOr<ListResponse>> GetPublicList(string listUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_apiEndpointUrl}/list/{listUrl}");
        var response = await SendAsync(request);
        if (response.IsError)
        {
            return response.FirstError;
        }

        var result = JsonSerializer.Deserialize<ListResponse>(response.Value, SerializerOptions);
        if (result is null)
        {
            return Error.Failure("Failed to deserialize json");
        }

        return result;
    }

    public async Task<ErrorOr<ListResponse>> GetList(string accessToken, string listUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_apiEndpointUrl}/list/{listUrl}");
        var response = await SendWithAuthAsync(request, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        var result = JsonSerializer.Deserialize<ListResponse>(response.Value, SerializerOptions);
        if (result is null)
        {
            return Error.Failure("Failed to deserialize json");
        }

        return result;
    }

    public async Task<ErrorOr<Success>> Delete(string accessToken, string listUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_apiEndpointUrl}/list/{listUrl}/delete");
        var response = await SendWithAuthAsync(request, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> BuyItem(string accessToken, string listUrl, long itemId, decimal price,
        long amount)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_apiEndpointUrl}/list/{listUrl}/buy-item?itemId={itemId}&price={price}&amount={amount}");
        var response = await SendWithAuthAsync(request, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> SellItem(string accessToken, string listUrl, long itemId, decimal price,
        long amount)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_apiEndpointUrl}/list/{listUrl}/sell-item?itemId={itemId}&price={price}&amount={amount}");
        var response = await SendWithAuthAsync(request, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }

    #endregion

    public async Task<ErrorOr<List<ItemSearchResponse>>> Search(string searchString, string accessToken)
    {
        var encodedSearchString = HttpUtility.UrlEncode(searchString);
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_apiEndpointUrl}/items/search?searchString={encodedSearchString}");
        var response = await SendWithAuthAsync(request, accessToken);
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