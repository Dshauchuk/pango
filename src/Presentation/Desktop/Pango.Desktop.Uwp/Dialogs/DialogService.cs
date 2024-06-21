using CommunityToolkit.Mvvm.Input;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.Views;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.Dialogs;

public class DialogService : IDialogService
{
    public Task ShowNewCatalogDialog(EditCatalogParameters catalogParameters)
    {
        return ShowAsync(new EditPasswordCatalogDialog(catalogParameters));
    }

    /// <summary>
    /// Raises a simple confirmation dialog, returns true if user clicked on the primary button, otherwise - false
    /// </summary>
    /// <param name="confirmationTitle"></param>
    /// <param name="confirmationText"></param>
    /// <returns></returns>
    public async Task<bool> ConfirmAsync(string confirmationTitle, string confirmationText)
    {
        var viewResourceLoader = new ResourceLoader();
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

    private async Task ShowAsync(IContentDialog dialogContent)
    {
        var viewResourceLoader = new ResourceLoader();

        ContentDialog dialog = new()
        {
            XamlRoot = Window.Current.Content.XamlRoot,
            Style = Microsoft.UI.Xaml.Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = dialogContent.Title,
            PrimaryButtonText = viewResourceLoader.GetString("Save"),
            CloseButtonText = viewResourceLoader.GetString("Cancel"),
            PrimaryButtonCommand = new RelayCommand(async () => await dialogContent.ViewModel.OnSaveAsync()),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,

            // initially define the primary button availability
            IsPrimaryButtonEnabled = dialogContent.ViewModel.CanSave()
        };

        // register a handler for any change of the dialog content
        dialogContent.ViewModel.DialogContext.OnContentChanged += DialogContext_OnContentChanged;
        void DialogContext_OnContentChanged(object sender, EventArgs e)
        {
            dialog.IsPrimaryButtonEnabled = dialogContent.ViewModel.CanSave();
        }

        dialog.Opened += dialogContent.DialogOpened;

        _ = await dialog.ShowAsync();
    }
}
