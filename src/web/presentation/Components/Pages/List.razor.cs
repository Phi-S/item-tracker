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
        try
        {
            await GetList();
            AddItemActionModalRef.ItemActionAdded += async (_, _) =>
            {
                try
                {
                    await GetList();
                    if (List is not null)
                    {
                        await ListDisplayRef.UpdateDiagram(List);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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

    protected Task AddBuyAction()
    {
        return AddItemActionModalRef.ShowBuy();
    }

    protected Task AddSellAction(ListItemResponse item)
    {
        return AddItemActionModalRef.ShowSell(item);
    }

    protected Task ShowItemActions(ListItemResponse item)
    {
        return ShowItemActionsModalRef.Show(item);
    }

    protected static string GetPerformanceString(long buyPrice, long? currentPrice)
    {
        var performance = Math.Round((double)(currentPrice ?? 0) / buyPrice * 100 - 100, 2);
        return performance > 0 ? $"+{performance}" : $"{performance}";
    }
}