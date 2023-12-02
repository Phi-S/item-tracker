using System.Text.Json.Serialization;
using BlazorBootstrap;

namespace presentation;

public class LineChartOptionsExtension : ChartOptions
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string IndexAxis { get; set; } = "x";

    public Interaction Interaction { get; set; } = new();

    public ChartLayout Layout { get; set; } = new();

    public LineChartPlugins Plugins { get; set; } = new();

    public ScalesEx Scales { get; set; } = new();
}

public class ScalesEx
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChartAxesEx? X { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChartAxesEx? Y { get; set; } = new();
}

public class ChartAxesEx
{
    public bool Display { get; set; } = false;

    public bool BeginAtZero { get; set; } = true;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Max { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Min { get; set; }

    public bool Stacked { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? SuggestedMax { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? SuggestedMin { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChartAxesTitle? Title { get; set; } = new();
}