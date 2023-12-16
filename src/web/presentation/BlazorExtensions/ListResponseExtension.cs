using System.Security.Claims;
using presentation.Authentication;
using shared.Models.ListResponse;

namespace presentation.BlazorExtensions;

public static class ListResponseExtension
{
    public static long Profit(this ListResponse list)
    {
        return list.Items.Sum(item => item.Profit);
    }
    
    public static int ItemCount(this ListResponse list)
    {
        return list.Items.Sum(item => item.ItemCount);
    }

    public static long InvestedCapital(this ListResponse list)
    {
        return list.Items.Sum(item => item.InvestedCapital);
    }

    public static long SteamPrice(this ListResponse list)
    {
        return list.Items.Sum(item => (item.SteamSellPrice ?? 0) * item.ItemCount);
    }
    
    public static long Buff163Price(this ListResponse list)
    {
        return list.Items.Sum(item => (item.Buff163SellPrice ?? 0) * item.ItemCount);
    }

    public static string GetPerformancePercentString(this ListResponse list)
    {
        var performance =
            Math.Round((double)(list.SteamPrice() - list.InvestedCapital()) / list.InvestedCapital() * 100, 2);
        return performance > 0 ? $"+{performance}%" : $"{performance}%";
    }

    public static string GetPerformancePercentString(this ListItemResponse item)
    {
        var performance = Math.Round((double)(item.SteamSellPrice ?? 0) / item.AverageBuyPrice * 100 - 100, 2);
        return performance > 0 ? $"+{performance}" : $"{performance}";
    }

    public static long GetPerformanceValueString(this ListItemResponse item)
    {
        return ((item.SteamSellPrice ?? 0) - item.AverageBuyPrice) * item.ItemCount;
    }
}