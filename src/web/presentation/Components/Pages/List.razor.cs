using infrastructure.ItemTrackerApi;
using Microsoft.AspNetCore.Components;
using presentation.Authentication;
using presentation.Components.Custom;
using shared.Models;
using shared.Models.ListResponse;

namespace presentation.Components.Pages;

public class ListRazor : ComponentBase
{
    [Inject] private CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ItemTrackerApiService ItemTrackerApiService { get; set; } = default!;

    [Parameter, EditorRequired] public string ListUrl { get; set; } = null!;

    protected Modal? ModalRef { get; set; }
    protected ErrorComponent ErrorComponentRef = null!;
    protected ListResponse? List;


    private long? _addEntrySelectedItemId;
    protected decimal? AddEntryPrice;
    protected long? AddEntryAmount;

    protected bool OpenSellModal;
    protected ListItemResponse? SellItem;

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

    protected Task AddEntryOnItemSelected(long itemId)
    {
        _addEntrySelectedItemId = itemId;
        AddEntryAmount = 1;
        AddEntryPrice = 0;
        StateHasChanged();
        return Task.CompletedTask;
    }

    protected void OpenModalBuyEntry()
    {
        SellItem = null;
        OpenNewEntryModal(true);
    }

    protected void OpenModalSellEntry(ListItemResponse item)
    {
        SellItem = item;
        OpenNewEntryModal(false);
    }

    protected void OpenNewEntryModal(bool buySell)
    {
        if (ModalRef is null)
        {
            return;
        }

        var buySellString = buySell ? "Buy" : "Sell";
        ModalRef.Title = $"{buySellString}";
        ModalRef.OkButtonString = ModalRef.Title;
        ModalRef.OkButtonAction = async () =>
        {
            var accessToken = AuthenticationStateProvider.Token?.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception("Failed to add entry. No access token found");
            }

            if (_addEntrySelectedItemId is null)
            {
                throw new Exception("Failed to add entry. No item selected");
            }

            if (AddEntryPrice is null)
            {
                throw new Exception("Failed to add entry. No price entered");
            }

            if (AddEntryAmount is null)
            {
                throw new Exception("Failed to add entry. No amount entered");
            }

            if (buySell)
            {
                var buyItem = await ItemTrackerApiService.BuyItem(
                    accessToken,
                    ListUrl,
                    _addEntrySelectedItemId.Value,
                    AddEntryPrice.Value,
                    AddEntryAmount.Value
                );
                if (buyItem.IsError)
                {
                    throw new Exception($"Failed to add entry. {buyItem.FirstError.Description}");
                }
            }
            else
            {
                var sellItem = await ItemTrackerApiService.SellItem(
                    accessToken,
                    ListUrl,
                    _addEntrySelectedItemId.Value,
                    AddEntryPrice.Value,
                    AddEntryAmount.Value
                );
                if (sellItem.IsError)
                {
                    throw new Exception($"Failed to add entry. {sellItem.FirstError.Description}");
                }
            }

            ModalRef.Close();
            await GetList();
        };

        ModalRef.Open();
    }

    protected async Task Delete(ListItemResponse item)
    {
        // TODO: implement delete...
    }
}