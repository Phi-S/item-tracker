﻿using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using presentation.Authentication;
using presentation.BlazorExtensions;
using presentation.Helper;
using presentation.ItemTrackerApi;
using shared.Models.ListResponse;
using Throw;

namespace presentation.Components.Custom;

public class ListDisplayRazor : ComponentBase
{
    [Inject] public CognitoAuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] protected ToastService ToastService { get; set; } = default!;
    [Inject] public ItemTrackerApiService ItemTrackerApiService { get; set; } = default!;

    [Parameter] [EditorRequired] public ListResponse List { get; set; } = default!;
    [Parameter] public bool DisplayGoToListButton { get; set; } = true;

    protected ConfirmDialog ConfirmDialogRef { get; set; } = default!;
    protected LineChart LineChartRef { get; set; } = default!;

    protected bool IsOwnList { get; private set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var userInfo = await AuthenticationStateProvider.UserInfo();
            IsOwnList = List.UserId.Equals(userInfo?.UserId);
            await RenderDiagram(List);
            StateHasChanged();
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

        if (List.Snapshots.Count != 0)
        {
            foreach (var listValue in listResponse.Snapshots.Where(snapshot =>
                         snapshot.CreatedUtc >= DateOnly.FromDateTime(DateTime.UtcNow.Subtract(TimeSpan.FromDays(30)))))
            {
                dataLabels.Add(listValue.CreatedUtc.ToString("yyyy-MM-dd"));
                steamPriceValues.Add(listValue.SteamSellPrice is null
                    ? 0
                    : CurrencyHelper.ToDouble(listResponse.Currency, listValue.SteamSellPrice.Value));
                buffPriceValues.Add(listValue.Buff163SellPrice is null
                    ? 0
                    : CurrencyHelper.ToDouble(listResponse.Currency, listValue.Buff163SellPrice.Value));
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
                PointBackgroundColor = ["#b3bab5"],
                PointRadius = [5],
                PointHoverRadius = [8]
            },
            new LineChartDataset
            {
                Label = "Steam price",
                Data = steamPriceValues,
                BorderColor = new List<string> { "#fcba03" },
                BorderWidth = new List<double> { 2 },
                HoverBorderWidth = new List<double> { 4 },
                PointBackgroundColor = ["#fcba03"],
                PointRadius = [5],
                PointHoverRadius = [8]
            },
            new LineChartDataset
            {
                Label = "Buff price",
                Data = buffPriceValues,
                BorderColor = new List<string> { "#4842f5" },
                BorderWidth = new List<double> { 2 },
                HoverBorderWidth = new List<double> { 4 },
                PointBackgroundColor = ["#4842f5"],
                PointRadius = [5],
                PointHoverRadius = [8]
            }
        };

        var chartData = new ChartData { Labels = dataLabels, Datasets = dataset };
        var lineChartOptions = new LineChartOptionsExtension
        {
            MaintainAspectRatio = false,
            Responsive = true,
            Scales = new ScalesEx
            {
                X = new ChartAxesEx
                {
                    BeginAtZero = true
                }
            },
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
            LineChartRef.ElementId!,
            "line",
            data,
            diagramData.lineChartOptionsExtension,
            null
        );
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
            LineChartRef.ElementId!,
            "line",
            data,
            diagramData.lineChartOptionsExtension
        );
    }

    protected async Task MakeListPublic()
    {
        var confirmation = await ConfirmDialogRef.Show(
            List.Name,
            "Public lists can be seen by everyone.",
            "Are you sure you want to make the list public?"
        );
        if (confirmation)
        {
            var userInfo = await AuthenticationStateProvider.UserInfo();
            var makeListPublic = await ItemTrackerApiService.UpdatePublic(userInfo?.AccessToken, List.Url, true);
            if (makeListPublic.IsError)
            {
                ToastService.Error(
                    $"Failed to set list \"{List.Name}\" to public. {makeListPublic.FirstError.Description}");
            }
            else
            {
                ToastService.Info($"List \"{List.Name}\" is now public");
                List = List with { Public = true };
                StateHasChanged();
            }
        }
    }

    protected async Task MakeListPrivate()
    {
        var confirmation = await ConfirmDialogRef.Show(
            List.Name,
            "Private lists can only be seen by yourself.",
            $"Are you sure you want to make the list \"{List.Name}\" private?"
        );
        if (confirmation)
        {
            var userInfo = await AuthenticationStateProvider.UserInfo();
            var makeListPrivate = await ItemTrackerApiService.UpdatePublic(userInfo?.AccessToken, List.Url, false);
            if (makeListPrivate.IsError)
            {
                ToastService.Error(
                    $"Failed to set list \"{List.Name}\" to private. {makeListPrivate.FirstError.Description}");
            }
            else
            {
                ToastService.Info($"List \"{List.Name}\" is now private");
                List = List with { Public = false };
                StateHasChanged();
            }
        }
    }

    protected async Task DeleteList()
    {
        var confirmation = await ConfirmDialogRef.Show(
            "Are you sure you want to delete this list",
            $"List name: {List.Name}"
        );
        if (confirmation)
        {
            var userInfo = await AuthenticationStateProvider.UserInfo();
            var deleteList = await ItemTrackerApiService.Delete(userInfo?.AccessToken, List.Url);
            if (deleteList.IsError)
            {
                ToastService.Error($"Failed to delete list \"{List.Name}\". {deleteList.FirstError.Description}");
            }
            else
            {
                ToastService.Info($"List \"{List.Name}\" deleted");
                NavigationManager.NavigateToLists();
            }
        }
    }
}