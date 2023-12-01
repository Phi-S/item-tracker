using infrastructure.ItemTrackerApi;
using Microsoft.AspNetCore.Components;
using presentation.Authentication;
using shared.Models;

namespace presentation.Components.Custom;

public class SearchComponentRazor : ComponentBase
{
    [Parameter] [EditorRequired] public Func<long, Task> OnItemSelected { get; set; } = null!;

    [Inject] public CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    [Inject] public ItemTrackerApiService ItemTrackerApiService { get; set; } = null!;

    protected ElementReference SearchResponsesDivRef;
    protected readonly List<ItemSearchResponse> ItemSearchResponses = new();
    protected string HideSearchResponsesClass = "visually-hidden";

    protected string CurrentSearchBoxContent { get; set; } = "";

    private string? _searchBoxContent = "";

    private volatile bool _backgroundTaskRunning;
    private DateTime _lastInput;

    private void StartBackgroundTask()
    {
        _lastInput = DateTime.Now;
        if (_backgroundTaskRunning)
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                _backgroundTaskRunning = true;
                while (true)
                {
                    await Task.Delay(50);
                    var timeSinceLastInput = DateTime.Now - _lastInput;
                    if (timeSinceLastInput.TotalMilliseconds <= 1000)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(_searchBoxContent) || _searchBoxContent.Length < 3)
                    {
                        break;
                    }

                    var accessToken = AuthenticationStateProvider.Token?.AccessToken;
                    if (string.IsNullOrWhiteSpace(accessToken))
                    {
                        throw new Exception("No access token set");
                    }

                    var searchResult = await ItemTrackerApiService.Search(_searchBoxContent, accessToken);
                    if (searchResult.IsError)
                    {
                        throw new Exception($"Failed to get search result. {searchResult.FirstError.Description}");
                    }

                    ItemSearchResponses.Clear();
                    ItemSearchResponses.AddRange(searchResult.Value);
                    ShowSearchResponses();
                    StateHasChanged();
                    break;
                }
            }
            finally
            {
                _backgroundTaskRunning = false;
            }
        });
    }

    protected void OnInput(ChangeEventArgs obj)
    {
        Console.WriteLine($"#### {CurrentSearchBoxContent} | ");
        var searchText = obj.Value?.ToString()?.Trim();
        _searchBoxContent = searchText;
        StartBackgroundTask();
    }

    protected void OnSelect(ItemSearchResponse item)
    {
        Console.WriteLine("========");
        Console.WriteLine(CurrentSearchBoxContent);
        CurrentSearchBoxContent = item.Name;
        OnItemSelected.Invoke(item.Id);
        StateHasChanged();
    }

    protected async Task HideSearchResponses()
    {
        await Task.Delay(100);
        HideSearchResponsesClass = "visually-hidden";
        StateHasChanged();
    }

    protected void ShowSearchResponses()
    {
        if (ItemSearchResponses.Any() == false)
        {
            HideSearchResponsesClass = "visually-hidden";
            return;
        }

        HideSearchResponsesClass = "";
    }
}