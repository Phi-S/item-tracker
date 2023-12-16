using BlazorBootstrap;

namespace presentation.BlazorExtensions;

public static class ConfirmDialogExtension
{
    private static readonly ConfirmDialogOptions VerticallyCenteredOption = new()
        { IsVerticallyCentered = true, DialogCssClass = "text-center" };

    public static Task<bool> Show(this ConfirmDialog confirmDialog, string title, string message1,
        string message2)
    {
        return confirmDialog.ShowAsync(title, message1, message2, VerticallyCenteredOption);
    }

    public static Task<bool> Show(this ConfirmDialog confirmDialog, string title, string message)
    {
        return confirmDialog.ShowAsync(title, message, VerticallyCenteredOption);
    }
}