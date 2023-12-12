using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using presentation.Authentication;
using presentation.ItemTrackerApi;
using shared.Models;

namespace presentation.Components.Custom;

public class ItemSearchComponentRazor : ComponentBase
{
    [Inject] public CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] public ItemTrackerApiService ItemTrackerApiService { get; set; } = null!;

    public ItemSearchResponse? SelectedItemSearchResponse { get; private set; }
    protected InputText InputRef { get; set; } = null!;
    protected List<ItemSearchResponse>? ItemSearchResponses = null;
    protected string HideSearchResponsesClass = "visually-hidden";
    protected bool LockInput;
    
    private volatile bool _backgroundTaskRunning;
    private DateTime _lastInput;

    public void Reset(ItemSearchResponse? selectedItem = null, bool lockInput = false)
    {
        LockInput = lockInput;
        SelectedItemSearchResponse = selectedItem;
        InputRef.Value = SelectedItemSearchResponse is not null ? SelectedItemSearchResponse.Name : "";
        ItemSearchResponses = null;
        InvokeAsync(StateHasChanged);
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
                    if (timeSinceLastInput.TotalMilliseconds <= 1000)
                    {
                        continue;
                    }

                    var searchString = InputRef.Value;
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

    protected void OnInput(ChangeEventArgs obj)
    {
        var searchText = obj.Value?.ToString()?.Trim();
        InputRef.Value = searchText;
        StartBackgroundTask();
    }

    protected void OnSelect(ItemSearchResponse item)
    {
        InputRef.Value = item.Name;
        SelectedItemSearchResponse = item;
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
        if (ItemSearchResponses is null)
        {
            HideSearchResponsesClass = "visually-hidden";
            return;
        }

        HideSearchResponsesClass = "";
        StateHasChanged();
    }
}