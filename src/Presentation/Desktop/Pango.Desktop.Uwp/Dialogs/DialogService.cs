using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.Dialogs;

public class DialogService : IDialogService
{
    public async Task ShowAsync(IContentDialog dialogContent)
    {
        var viewResourceLoader = ResourceLoader.GetForCurrentView();
        
        ContentDialog dialog = new()
        {
            // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
            XamlRoot = Window.Current.Content.XamlRoot,
            Style = Windows.UI.Xaml.Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = dialogContent.Title,
            PrimaryButtonText = viewResourceLoader.GetString("Save"),
            CloseButtonText = viewResourceLoader.GetString("Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent
        };

        ContentDialogResult result = await dialog.ShowAsync();

        if(result == ContentDialogResult.Primary)
        {
            await dialogContent.ViewModel.OnSaveAsync();
        }
    }

    /// <summary>
    /// Raises a simple confirmation dialog, returns true if user clicked on the primary button, otherwise - false
    /// </summary>
    /// <param name="confirmationTitle"></param>
    /// <param name="confirmationText"></param>
    /// <returns></returns>
    public async Task<bool> ConfirmAsync(string confirmationTitle, string confirmationText)
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
