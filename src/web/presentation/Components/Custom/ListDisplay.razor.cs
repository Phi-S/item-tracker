using application.Helper;
using BlazorBootstrap;
using infrastructure.ItemTrackerApi;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using presentation.Authentication;
using presentation.BlazorExtensions;
using presentation.Components.Pages;
using shared.Models.ListResponse;

namespace presentation.Components.Custom;

public class ListDisplayRazor : ComponentBase
{
    [Parameter] [EditorRequired] public ListResponse List { get; set; } = null!;
    [Inject] public ILogger<ListDisplay> Logger { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    protected LineChart LineChart = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var timezoneOffsetH = await JsRuntime.GetBrowserTimezoneOffsetInH();

            var dataLabels = new List<string>();
            var values = new List<double>();

            if (List.ListValues.Any())
            {
                foreach (var listValue in List.ListValues)
                {
                    dataLabels.Add(listValue.CreatedAt.AddHours(timezoneOffsetH).ToString("yyyy-MM-dd HH:mm:ss"));
                    values.Add((double)(listValue.SteamValue ?? 0));
                }
            }
            else
            {
                dataLabels.Add(DateTime.UtcNow.AddHours(timezoneOffsetH).ToString("yyyy-MM-dd HH:mm:ss"));
                values.Add(0);
            }

            var c = ColorBuilder.CategoricalTwelveColors[values.Count].ToColor();
            var dataset = new List<IChartDataset>
            {
                new LineChartDataset
                {
                    Label = null,
                    Data = values,
                    BackgroundColor = new List<string> { c.ToRgbString() },
                    BorderColor = new List<string> { c.ToRgbString() },
                    BorderWidth = new List<double> { 2 },
                    HoverBorderWidth = new List<double> { 4 },
                    PointBackgroundColor = new List<string> { c.ToRgbString() },
                    PointRadius = new List<int> { 5 }, // hide points
                    PointHoverRadius = new List<int> { 8 }
                }
            };

            var chartData = new ChartData { Labels = dataLabels, Datasets = dataset };
            var lineChartOptions = new LineChartOptions
            {
                MaintainAspectRatio = false,
                Responsive = true, Interaction = new Interaction { Mode = InteractionMode.Index },
                Plugins = new LineChartPlugins
                {
                    Legend = new ChartPluginsLegend { Display = false }
                }
            };
            await LineChart.InitializeAsync(chartData, lineChartOptions);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected void NavigateToList()
    {
        NavigationManager.NavigateTo($"/list/{List.Url}");
    }

    protected async Task DeleteList(string listUrl)
    {
        //TODO: move delete somewhere else
        /*
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
        */
    }
}