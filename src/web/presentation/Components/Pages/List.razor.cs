using infrastructure.ItemTrackerApi;
using Microsoft.AspNetCore.Components;
using presentation.Authentication;
using presentation.Components.Custom;
using shared.Models.ListResponse;

namespace presentation.Components.Pages;

public class ListRazor : ComponentBase
{
    [Inject] private CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ItemTrackerApiService ItemTrackerApiService { get; set; } = default!;

    [Parameter, EditorRequired] public string ListUrl { get; set; } = null!;

    protected ErrorComponent ErrorComponentRef = null!;
    protected ListResponse? ListResponse;

    protected override async Task OnInitializedAsync()
    {
        var accessToken = AuthenticationStateProvider.Token?.AccessToken;
        var list = await ItemTrackerApiService.Get(accessToken, ListUrl);
        if (list.IsError)
        {
            ErrorComponentRef.SetError(list.FirstError.Description);
            return;
        }

        ListResponse = list.Value;
    }

    protected async Task BuyItem(long itemId)
    {
        var accessToken = AuthenticationStateProvider.Token?.AccessToken;
        await ItemTrackerApiService.BuyItem(accessToken, ListUrl, itemId, 5, 2);
        NavigationManager.Refresh();
    }
}