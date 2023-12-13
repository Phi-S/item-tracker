using BlazorBootstrap;

namespace presentation.BlazorExtensions;

public static class ToastServiceExtension
{
    public static void Error(this ToastService toastService, string message)
    {
        toastService.Notify(new ToastMessage(ToastType.Danger, message));
    }

    public static void Info(this ToastService toastService, string message)
    {
        toastService.Notify(new ToastMessage(ToastType.Info, message));
    }
}