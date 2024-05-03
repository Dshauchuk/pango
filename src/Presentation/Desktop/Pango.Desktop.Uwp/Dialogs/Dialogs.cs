using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.Dialogs;

public interface IContentDialog
{

}

public static class Dialogs
{
    public static async Task<bool> ConfirmAsync(string confirmationTitle, string confirmationText)
    {
        var viewResourceLoader = ResourceLoader.GetForCurrentView();
        ContentDialog subscribeDialog = new()
        {
            Title = confirmationTitle,
            Content = confirmationText,
            CloseButtonText = viewResourceLoader.GetString("Cancel"),
            PrimaryButtonText = viewResourceLoader.GetString("Yes"),
            DefaultButton = ContentDialogButton.Primary
        };

        ContentDialogResult result = await subscribeDialog.ShowAsync();

        return result == ContentDialogResult.Primary;
    }

    public static async Task ShowContentDialogAsync(IContentDialog contentDialog)
    {
        ContentDialog dialog = new ContentDialog();

        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
        dialog.XamlRoot = this.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = "Save your work?";
        dialog.PrimaryButtonText = "Save";
        dialog.SecondaryButtonText = "Don't Save";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;
        dialog.Content = new ContentDialogContent();

        var result = await dialog.ShowAsync();

    }
}
