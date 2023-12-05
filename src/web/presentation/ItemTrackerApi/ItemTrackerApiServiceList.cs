using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ErrorOr;
using presentation.Helper;
using shared.Models;
using shared.Models.ListResponse;

namespace presentation.ItemTrackerApi;

[SuppressMessage("Maintainability", "CA1507:Use nameof to express symbol names")]
public partial class ItemTrackerApiService
{
    public async Task<ErrorOr<List<ListResponse>>> All(string? accessToken)
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
            return Error.Failure(description: "Failed to deserialize json");
        }

        return result;
    }

    public async Task<ErrorOr<string>> New(string? accessToken, NewListModel newListModel)
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
            return Error.Failure(description: "Failed to deserialize json");
        }

        return result;
    }

    public async Task<ErrorOr<Success>> Delete(string? accessToken, string listUrl)
    {
        var url = $"{_apiEndpointUrl}/list/{listUrl}/delete";
        var response = await DeleteWithAuthAsync(url, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> BuyItem(
        string? accessToken,
        string listUrl,
        long itemId,
        long amount,
        decimal price)
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
        string? accessToken,
        string listUrl,
        long itemId,
        long amount,
        decimal price)
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

    public async Task<ErrorOr<Success>> UpdateName(
        string? accessToken,
        string listUrl,
        string newName)
    {
        var uri = new Uri($"{_apiEndpointUrl}/list/{listUrl}/update-name");
        uri = uri.AddParameter("newName", newName);

        var response = await PutWithAuthAsync(uri.AbsoluteUri, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> UpdateDescription(
        string? accessToken,
        string listUrl,
        string newDescription)
    {
        var uri = new Uri($"{_apiEndpointUrl}/list/{listUrl}/update-description");
        uri = uri.AddParameter("newDescription", newDescription);

        var response = await PutWithAuthAsync(uri.AbsoluteUri, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> UpdatePublic(
        string? accessToken,
        string listUrl,
        bool newPublic)
    {
        var uri = new Uri($"{_apiEndpointUrl}/list/{listUrl}/update-public");
        uri = uri.AddParameter("newPublic", newPublic.ToString());

        var response = await PutWithAuthAsync(uri.AbsoluteUri, accessToken);
        if (response.IsError)
        {
            return response.FirstError;
        }

        return Result.Success;
    }
}