using Microsoft.AspNetCore.Components;

namespace presentation.BlazorExtensions;

public static class NavigationManagerExtension
{
    public static void NavigateToList(this NavigationManager navigationManager, string listUrl)
    {
        navigationManager.NavigateTo($"/list/{listUrl}");
    }

    public static void NavigateToLists(this NavigationManager navigationManager)
    {
        navigationManager.NavigateTo("/lists");
    }
}