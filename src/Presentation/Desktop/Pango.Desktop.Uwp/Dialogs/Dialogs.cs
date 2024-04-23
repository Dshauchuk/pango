using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.Dialogs;

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
}
