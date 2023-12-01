using Microsoft.JSInterop;

namespace presentation.BlazorExtensions;

public static class JsRuntimeExtensionMethods
{
    public static async Task<int> GetBrowserTimezoneOffsetInH(this IJSRuntime jsRuntime)
    {
        var browserTimezone = await jsRuntime.InvokeAsync<int>("getBrowserTimezoneOffset");
        return browserTimezone;
    }
}