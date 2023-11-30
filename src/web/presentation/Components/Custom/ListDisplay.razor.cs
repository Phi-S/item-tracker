using ApexCharts;
using application.Helper;
using infrastructure.ItemTrackerApi;
using Microsoft.AspNetCore.Components;
using presentation.Authentication;
using shared.Models.ListResponse;

namespace presentation.Components.Custom;

public class ListDisplayRazor : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public ListResponse List { get; set; } = null!;

    [Inject]
    public ILogger<ListDisplay> Logger { get; set; } = null!;

    [Inject]
    public CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public ItemTrackerApiService ItemTrackerApiService { get; set; } = null!;

    protected string Name => $"List value in {CurrencyHelper.Get(List.Currency)}";

    protected readonly ApexChartOptions<ListValueResponse> ApexChartOptions = new()
    {
        Chart = new Chart
        {
            Toolbar = new Toolbar
            {
                Show = false
            }
        },
        Theme = new Theme
        {
            Mode = Mode.Dark,
            Palette = PaletteType.Palette1
        },
        Xaxis = new XAxis
        {
            Labels = new XAxisLabels
            {
                Show = false
            }
        }
    };

    protected void NavigateToList()
    {
        NavigationManager.NavigateTo($"/list/{List.Url}");
    }

    protected async Task DeleteList(string listUrl)
    {
        var accessToken = AuthenticationStateProvider.Token?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new Exception("Access token is not set. This should never happen");
        }

        var deleteList = await ItemTrackerApiService.Delete(accessToken, listUrl);
        if (deleteList.IsError)
        {
            Logger.LogError("Failed to delete list {ListUrl}. {Error}", listUrl, deleteList.FirstError.Description);
            //TODO: alert?
            return;
        }

        NavigationManager.Refresh();
    }
}