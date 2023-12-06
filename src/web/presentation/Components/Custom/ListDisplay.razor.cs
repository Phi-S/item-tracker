using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using presentation.BlazorExtensions;
using shared.Currencies;
using shared.Models.ListResponse;
using Throw;

namespace presentation.Components.Custom;

public class ListDisplayRazor : ComponentBase
{
    [Parameter] [EditorRequired] public ListResponse List { get; set; } = null!;
    [Parameter] public bool DisplayGoToListButton { get; set; } = true;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = null!;

    protected LineChart LineChart = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RenderDiagram(List);
            await base.OnAfterRenderAsync(firstRender);
        }
    }

    private async Task<(ChartData chartData, LineChartOptionsExtension lineChartOptionsExtension)> GetDiagramData(
        ListResponse listResponse)
    {
        var timezoneOffsetH = await JsRuntime.GetBrowserTimezoneOffsetInH();

        var dataLabels = new List<string>();
        var steamPriceValues = new List<double>();
        var buffPriceValues = new List<double>();
        var investedCapitalValues = new List<double>();

        if (List.ListSnapshots.Any())
        {
            foreach (var listValue in listResponse.ListSnapshots)
            {
                dataLabels.Add(listValue.CreatedAt.AddHours(timezoneOffsetH).ToString("yyyy-MM-dd HH:mm:ss"));
                steamPriceValues.Add(listValue.SteamValue is null
                    ? 0
                    : CurrencyHelper.ToDouble(listResponse.Currency, listValue.SteamValue.Value));
                buffPriceValues.Add(listValue.Buff163Value is null
                    ? 0
                    : CurrencyHelper.ToDouble(listResponse.Currency, listValue.Buff163Value.Value));
                investedCapitalValues.Add(CurrencyHelper.ToDouble(listResponse.Currency, listValue.InvestedCapital));
            }
        }
        else
        {
            dataLabels.Add(DateTime.UtcNow.AddHours(timezoneOffsetH).ToString("yyyy-MM-dd HH:mm:ss"));
            steamPriceValues.Add(0);
            buffPriceValues.Add(0);
            investedCapitalValues.Add(0);
        }

        var dataset = new List<IChartDataset>
        {
            new LineChartDataset
            {
                Label = "Invested capital",
                Data = investedCapitalValues,
                BorderColor = new List<string> { "#b3bab5" },
                BorderWidth = new List<double> { 2 },
                HoverBorderWidth = new List<double> { 4 },
                PointBackgroundColor = new List<string> { "#b3bab5" },
                PointRadius = new List<int> { 5 },
                PointHoverRadius = new List<int> { 8 }
            },
            new LineChartDataset
            {
                Label = "Steam price",
                Data = steamPriceValues,
                BorderColor = new List<string> { "#fcba03" },
                BorderWidth = new List<double> { 2 },
                HoverBorderWidth = new List<double> { 4 },
                PointBackgroundColor = new List<string> { "#fcba03" },
                PointRadius = new List<int> { 5 },
                PointHoverRadius = new List<int> { 8 }
            },
            new LineChartDataset
            {
                Label = "Buff price",
                Data = buffPriceValues,
                BorderColor = new List<string> { "#4842f5" },
                BorderWidth = new List<double> { 2 },
                HoverBorderWidth = new List<double> { 4 },
                PointBackgroundColor = new List<string> { "#4842f5" },
                PointRadius = new List<int> { 5 },
                PointHoverRadius = new List<int> { 8 }
            }
        };

        var chartData = new ChartData { Labels = dataLabels, Datasets = dataset };
        var lineChartOptions = new LineChartOptionsExtension
        {
            MaintainAspectRatio = false,
            Responsive = true,
            Interaction = new Interaction
            {
                Mode = InteractionMode.Index
            },
            Layout = new ChartLayout
            {
                Padding = 0,
                AutoPadding = false
            },
            Plugins = new LineChartPlugins
            {
                Title = new ChartPluginsTitle
                {
                    Display = false
                },
                Legend = new ChartPluginsLegend
                {
                    Display = true,
                    Position = "top",
                    Align = "start"
                }
            }
        };


        return (chartData, lineChartOptions);
    }

    private async Task RenderDiagram(ListResponse listResponse)
    {
        var diagramData = await GetDiagramData(listResponse);
        diagramData.chartData.Datasets.ThrowIfNull();

        var data = new
        {
            diagramData.chartData.Labels,
            Datasets = diagramData.chartData.Datasets.OfType<LineChartDataset>()
        };

        await JsRuntime.InvokeVoidAsync(
            "window.blazorChart.line.initialize",
            LineChart.ElementId!,
            "line",
            data,
            diagramData.lineChartOptionsExtension,
            null
        );
        StateHasChanged();
    }

    public async Task UpdateDiagram(ListResponse listResponse)
    {
        var diagramData = await GetDiagramData(listResponse);
        diagramData.chartData.Datasets.ThrowIfNull();
        var data = new
        {
            diagramData.chartData.Labels,
            Datasets = diagramData.chartData.Datasets.OfType<LineChartDataset>()
        };
        await JsRuntime.InvokeVoidAsync(
            "window.blazorChart.line.update",
            LineChart.ElementId!,
            "line",
            data,
            diagramData.lineChartOptionsExtension
        );
        StateHasChanged();
    }

    protected void NavigateToList()
    {
        NavigationManager.NavigateTo($"/list/{List.Url}");
    }
}