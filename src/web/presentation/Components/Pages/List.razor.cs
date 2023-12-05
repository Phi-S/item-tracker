using Microsoft.AspNetCore.Components;
using presentation.Authentication;
using presentation.Components.Custom;
using presentation.ItemTrackerApi;
using shared.Models;
using shared.Models.ListResponse;

namespace presentation.Components.Pages;

public class ListRazor : ComponentBase
{
    [Inject] private CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ItemTrackerApiService ItemTrackerApiService { get; set; } = default!;

    [Parameter, EditorRequired] public string ListUrl { get; set; } = null!;

    protected ListResponse? List;

    protected ListDisplay ListDisplayRef { get; set; } = null!;
    protected Modal AddEntryModalRef { get; set; } = null!;
    protected ItemSearchComponent AddEntrySearchComponentRef { get; set; } = null!;

    protected ErrorComponent ErrorComponentRef = null!;
    protected string? ErrorMessage;


    protected long? AddEntryAmount;
    protected decimal? AddEntryPrice;

    protected override async Task OnInitializedAsync()
    {
        await GetList();
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

    protected void OpenModalBuyEntry()
    {
        ErrorMessage = null;
        AddEntrySearchComponentRef.Reset();
        OpenNewEntryModal(true);
    }

    protected void OpenModalSellEntry(ListItemResponse item)
    {
        ErrorMessage = null;
        AddEntrySearchComponentRef.Reset(
            new ItemSearchResponse(item.ItemId, item.ItemName, item.ItemImage),
            true
        );
        OpenNewEntryModal(false);
    }

    private void OpenNewEntryModal(bool buySell)
    {
        AddEntryAmount = null;
        AddEntryPrice = null;
        var buySellString = buySell ? "Buy" : "Sell";
        AddEntryModalRef.Title = $"{buySellString}";
        AddEntryModalRef.OkButtonString = AddEntryModalRef.Title;
        AddEntryModalRef.OkButtonAction = async () =>
        {
            var accessToken = AuthenticationStateProvider.Token?.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                ErrorMessage = "No access token found";
                return;
            }

            var selectedItem = AddEntrySearchComponentRef.SelectedItemSearchResponse;
            var amount = AddEntryAmount;
            var price = AddEntryPrice;
            if (selectedItem is null)
            {
                ErrorMessage = "No item selected";
                return;
            }

            if (amount is null or <= 0)
            {
                ErrorMessage = "No amount entered";
                return;
            }

            if (price is null or <= 0)
            {
                ErrorMessage = "No price entered";
                return;
            }

            if (buySell)
            {
                var buyItem = await ItemTrackerApiService.BuyItem(
                    accessToken,
                    ListUrl,
                    selectedItem.Id,
                    amount.Value,
                    price.Value
                );
                if (buyItem.IsError)
                {
                    ErrorMessage = $"{buyItem.FirstError.Description}";
                    return;
                }
            }
            else
            {
                var itemInList = List?.Items.FirstOrDefault(item => item.ItemId == selectedItem.Id);
                if (itemInList is null)
                {
                    ErrorMessage = "You can't sell an item you dont have";
                    return;
                }

                if (amount > itemInList.CurrentAmountInvested)
                {
                    ErrorMessage = "You can't sell more items than the list contains";
                    return;
                }

                var sellItem = await ItemTrackerApiService.SellItem(
                    accessToken,
                    ListUrl,
                    selectedItem.Id,
                    amount.Value,
                    price.Value
                );
                if (sellItem.IsError)
                {
                    ErrorMessage = $"{sellItem.FirstError.Description}";
                    return;
                }
            }

            AddEntryModalRef.Close();
            await GetList();
            await ListDisplayRef.UpdateDiagram(List!);
        };

        AddEntryModalRef.Open();
    }
}