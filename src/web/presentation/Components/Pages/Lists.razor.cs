using Microsoft.AspNetCore.Components;
using presentation.Authentication;
using presentation.Components.Custom;
using presentation.ItemTrackerApi;
using shared.Models;
using shared.Models.ListResponse;
using Throw;

namespace presentation.Components.Pages;

public class ListsRazor : ComponentBase
{
    [Inject] public ILogger<ListRazor> Logger { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [Inject] public ItemTrackerApiService ItemTrackerApiService { get; set; } = null!;

    protected ErrorComponent ErrorComponentRef { get; set; } = null!;
    protected ModalCustom NewListModalCustomRef { get; set; } = null!;
    protected List<ListResponse>? Lists;

    protected NewListModel NewListModel = new();

    protected override async Task OnInitializedAsync()
    {
        var accessToken = AuthenticationStateProvider.Token?.AccessToken;
        accessToken.ThrowIfNull().IfEmpty().IfWhiteSpace();

        var list = await ItemTrackerApiService.All(accessToken);
        if (list.IsError)
        {
            ErrorComponentRef.SetError(list.FirstError.Description);
            return;
        }

        NewListModel.Currency = "EUR";
        Lists = list.Value;
    }

    protected void NavigateToList(string listUrl)
    {
        NavigationManager.NavigateTo($"/list/{listUrl}");
    }

    protected async Task NewList()
    {
        var accessToken = AuthenticationStateProvider.Token?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new Exception("Access token is not set. This should never happen");
        }

        var newList = await ItemTrackerApiService.New(accessToken, NewListModel);
        if (newList.IsError == false)
        {
            NewListModel = new NewListModel();
            NavigateToList(newList.Value);
        }
        else
        {
            Logger.LogError("Failed to create new list. {Error}", newList.FirstError.Description);
        }
    }

    protected void OpenNewListModal()
    {
        NewListModalCustomRef.Open();
    }
}