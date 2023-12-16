using Microsoft.AspNetCore.Components;
using presentation.Authentication;
using presentation.Components.Custom;
using presentation.ItemTrackerApi;
using shared.Models.ListResponse;

namespace presentation.Components.Pages;

public class ListRazor : ComponentBase
{
    [Inject] private CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ItemTrackerApiService ItemTrackerApiService { get; set; } = default!;

    [Parameter, EditorRequired] public string ListUrl { get; set; } = default!;
    protected ErrorComponent ErrorComponentRef { get; set; } = default!;
    protected ListDisplay ListDisplayRef { get; set; } = default!;
    protected AddItemActionModal AddItemActionModalRef { get; set; } = default!;
    protected ShowItemActionsModal ShowItemActionsModalRef { get; set; } = default!;

    protected ListResponse? List;
    protected bool IsOwnList;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var accessToken = AuthenticationStateProvider.Token?.AccessToken;
            var list = await ItemTrackerApiService.Get(accessToken, ListUrl);
            if (list.IsError)
            {
                ErrorComponentRef.SetError(list.FirstError.Description);
                return;
            }

            var authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            IsOwnList = authenticationState.User.IsOwnList(list.Value);
            List = list.Value;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        await base.OnInitializedAsync();
    }

    private async Task RefreshList()
    {
        try
        {
            var accessToken = AuthenticationStateProvider.Token?.AccessToken;
            var list = await ItemTrackerApiService.Get(accessToken, ListUrl);
            if (list.IsError)
            {
                ErrorComponentRef.SetError(list.FirstError.Description);
                return;
            }

            List = list.Value;
            await ListDisplayRef.UpdateDiagram(list.Value);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected async Task AddBuyAction()
    {
        try
        {
            await AddItemActionModalRef.ShowBuy(RefreshList);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected async Task AddSellAction(ListItemResponse item)
    {
        try
        {
            await AddItemActionModalRef.ShowSell(item, RefreshList);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    protected async Task ShowItemActions(ListItemResponse item)
    {
        try
        {
            await ShowItemActionsModalRef.Show(item, RefreshList);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}