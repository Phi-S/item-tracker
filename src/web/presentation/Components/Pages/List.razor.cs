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

    [Parameter, EditorRequired] public string ListUrl { get; set; } = null!;

    protected ErrorComponent ErrorComponentRef = null!;
    protected ListDisplay ListDisplayRef { get; set; } = null!;
    protected AddItemActionModal AddItemActionModalRef { get; set; } = default!;
    protected ShowItemActionsModal ShowItemActionsModalRef { get; set; } = default!;


    protected ListResponse? List;

    protected override async Task OnInitializedAsync()
    {
        await GetList();
        AddItemActionModalRef.ItemActionAdded += async (_, _) =>
        {
            await GetList();
            if (List is not null)
            {
                await ListDisplayRef.UpdateDiagram(List);
            }
        };
    }

    private async Task GetList()
    {
        var accessToken = AuthenticationStateProvider.Token?.AccessToken;
        var list = await ItemTrackerApiService.Get(accessToken, ListUrl);
        if (list.IsError)
        {
            ErrorComponentRef.SetError(list.FirstError.Description);
            return;
        }

        List = list.Value;
        StateHasChanged();
    }

    protected async Task AddBuyAction()
    {
        await AddItemActionModalRef.ShowBuy();
    }

    protected async Task AddSellAction(ListItemResponse item)
    {
        await AddItemActionModalRef.ShowSell(item);
    }

    protected async Task ShowItemActions(ListItemResponse item)
    {
        await ShowItemActionsModalRef.Show(item);
    }
}