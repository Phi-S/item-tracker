using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ErrorOr;
using shared.Models;
using Throw;

namespace presentation.ItemTrackerApi;

public partial class ItemTrackerApiService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _apiEndpointUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ItemTrackerApiService> _logger;

    public ItemTrackerApiService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<ItemTrackerApiService> logger)
    {
        var apiEndpointUrl = configuration.GetValue<string>("ITEM_TRACKER_API_ENDPOINT");
        apiEndpointUrl.ThrowIfNull().IfEmpty().IfWhiteSpace();

        _apiEndpointUrl = apiEndpointUrl;
        _httpClient = httpClient;
        _logger = logger;
    }

    private async Task<ErrorOr<string>> DeleteWithAuthAsync(string url, string? accessToken)
    {
        return await SendWithAuthAsync(new HttpRequestMessage(HttpMethod.Delete, url), accessToken);
    }

    private async Task<ErrorOr<string>> PutWithAuthAsync(string url, string? accessToken)
    {
        return await SendWithAuthAsync(new HttpRequestMessage(HttpMethod.Put, url), accessToken);
    }

    private async Task<ErrorOr<string>> GetWithAuthAsync(string url, string? accessToken)
    {
        return await SendWithAuthAsync(new HttpRequestMessage(HttpMethod.Get, url), accessToken);
    }

    private async Task<ErrorOr<string>> PostWithAuthAsync(string url, string? accessToken)
    {
        return await SendWithAuthAsync(new HttpRequestMessage(HttpMethod.Post, url), accessToken);
    }

    private async Task<ErrorOr<string>> PostWithAuthAsync(string url, HttpContent content, string? accessToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            url
        );
        request.Content = content;
        return await SendWithAuthAsync(request, accessToken);
    }

    private async Task<ErrorOr<string>> SendWithAuthAsync(HttpRequestMessage requestMessage, string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Error.Unauthorized(description: "Access token is not valid");
        }

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await SendAsync(requestMessage);
    }

    private async Task<ErrorOr<string>> GetAsync(string url)
    {
        return await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
    }

    private async Task<ErrorOr<string>> SendAsync(HttpRequestMessage requestMessage)
    {
        try
        {
            requestMessage.Headers.Add("accept", "application/json");
            requestMessage.Headers.Add("Access-Control-Request-Headers", "content-type");
            var response = await _httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode == false)
            {
                var message = $"Request failed with status code {response.StatusCode}";
                var responseContentAsString = await response.Content.ReadAsStringAsync();
                var errorResponseModel = JsonSerializer.Deserialize<ErrorResultModel>(responseContentAsString);
                if (errorResponseModel is null)
                {
                    return Error.Failure(description: message);
                }

                return response.StatusCode == HttpStatusCode.Unauthorized
                    ? Error.Unauthorized(description: $"{errorResponseModel.Message}")
                    : Error.Failure(description: $"{errorResponseModel.Message}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return json;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Request failed\n{Request}", requestMessage);
            return Error.Failure(description: "Request failed");
        }
    }
}