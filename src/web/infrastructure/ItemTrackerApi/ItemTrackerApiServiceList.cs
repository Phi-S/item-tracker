using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using application;
using ErrorOr;
using shared.Models;
using shared.Models.ListResponse;

namespace infrastructure.ItemTrackerApi;

[SuppressMessage("Maintainability", "CA1507:Use nameof to express symbol names")]
public partial class ItemTrackerApiService
{
    public async Task<ErrorOr<List<ListResponse>>> All(string accessToken)
    {
        var url = $"{_apiEndpointUrl}/list/all";
        var response = await GetWithAuthAsync(url, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        var result = JsonSerializer.Deserialize<List<ListResponse>>(response.Value, SerializerOptions);
        if (result is null)
        {
            return Error.Failure("Failed to deserialize json");
        }

        return result;
    }

    public async Task<ErrorOr<string>> New(string accessToken, NewListModel newListModel)
    {
        var newListModelJson = JsonSerializer.Serialize(newListModel);
        var url = $"{_apiEndpointUrl}/list/new";
        var content = new StringContent(newListModelJson, Encoding.UTF8, "application/json");

        var response = await PostWithAuthAsync(url, content, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return response.Value;
    }

    public async Task<ErrorOr<Success>> Delete(string accessToken, string listUrl)
    {
        var url = $"{_apiEndpointUrl}/list/{listUrl}/delete";
        var response = await DeleteWithAuthAsync(url, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<ListResponse>> Get(string? accessToken, string listUrl)
    {
        var url = $"{_apiEndpointUrl}/list/{listUrl}";
        var response = string.IsNullOrWhiteSpace(accessToken)
            ? await GetAsync(url)
            : await GetWithAuthAsync(url, accessToken);
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

    public async Task<ErrorOr<Success>> BuyItem(
        string accessToken,
        string listUrl,
        long itemId,
        decimal price,
        long amount)
    {
        var uri = new Uri($"{_apiEndpointUrl}/list/{listUrl}/buy-item");
        uri = uri.AddParameter("itemId", itemId.ToString());
        uri = uri.AddParameter("price", price.ToString(CultureInfo.InvariantCulture));
        uri = uri.AddParameter("amount", amount.ToString());

        var response = await PostWithAuthAsync(uri.AbsoluteUri, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> SellItem(
        string accessToken,
        string listUrl,
        long itemId,
        decimal price,
        long amount)
    {
        var uri = new Uri($"{_apiEndpointUrl}/list/{listUrl}/sell-item");
        uri = uri.AddParameter("itemId", itemId.ToString());
        uri = uri.AddParameter("price", price.ToString(CultureInfo.InvariantCulture));
        uri = uri.AddParameter("amount", amount.ToString());

        var response = await PostWithAuthAsync(uri.AbsoluteUri, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }
}