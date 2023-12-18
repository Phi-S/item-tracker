using Microsoft.AspNetCore.Components;
using presentation.Authentication;
using presentation.ItemTrackerApi;
using shared.Models;

namespace presentation.Components.Custom;

public class ItemSearchComponentRazor : ComponentBase
{
    [Inject] public CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public ItemTrackerApiService ItemTrackerApiService { get; set; } = default!;

    public ItemSearchResponse? SelectedItemSearchResponse { get; private set; }
    protected List<ItemSearchResponse>? ItemSearchResponses;
    protected string HideSearchResponsesClass = "visually-hidden";
    protected bool LockInput;

    private string _searchInputText = "";

    protected string SearchInputText
    {
        get => _searchInputText;
        set
        {
            _searchInputText = value;
            StartBackgroundTask();
        }
    }

    private volatile bool _backgroundTaskRunning;
    private DateTime _lastInput;

    public void Reset(ItemSearchResponse? selectedItem = null, bool lockInput = false)
    {
        LockInput = lockInput;
        SelectedItemSearchResponse = selectedItem;
        SearchInputText = SelectedItemSearchResponse is not null ? SelectedItemSearchResponse.Name : "";
        ItemSearchResponses = null;
        StateHasChanged();
    }

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
                    if (timeSinceLastInput.TotalMilliseconds <= 600)
                    {
                        continue;
                    }

                    var searchString = SearchInputText;
                    if (string.IsNullOrWhiteSpace(searchString) || searchString.Length < 3)
                    {
                        break;
                    }

                    var accessToken = AuthenticationStateProvider.Token?.AccessToken;
                    if (string.IsNullOrWhiteSpace(accessToken))
                    {
                        throw new Exception("No access token set");
                    }

                    var searchResult = await ItemTrackerApiService.Search(searchString, accessToken);
                    if (searchResult.IsError)
                    {
                        throw new Exception($"Failed to get search result. {searchResult.FirstError.Description}");
                    }

                    ItemSearchResponses = searchResult.Value;
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

    protected void OnSelect(ItemSearchResponse item)
    {
        Console.WriteLine($"OnSelect: {item}");
        SelectedItemSearchResponse = item;
        _searchInputText = SelectedItemSearchResponse is not null ? SelectedItemSearchResponse.Name : "";
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
        if (LockInput)
        {
            HideSearchResponsesClass = "visually-hidden";
            return;
        }

        HideSearchResponsesClass = "";
        StateHasChanged();
    }
}