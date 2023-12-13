﻿using Microsoft.AspNetCore.Components;
using presentation.Authentication;
using presentation.Components.Custom;
using presentation.ItemTrackerApi;
using shared.Models.ListResponse;
using Throw;

namespace presentation.Components.Pages;

public class ListsRazor : ComponentBase
{
    [Inject] public CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public ItemTrackerApiService ItemTrackerApiService { get; set; } = default!;

    protected ErrorComponent ErrorComponentRef { get; set; } = default!;
    protected CreateNewListModal CreateNewListModalRef { get; set; } = default!;

    protected List<ListResponse>? Lists;


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

        Lists = list.Value;
    }

    protected Task OpenNewListModal()
    {
        return CreateNewListModalRef.Show();
    }
}