using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.Views;
using Pango.Desktop.Uwp.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.Dialogs;

public class DialogService : IDialogService
{
    public Task ShowNewCatalogDialogAsync(EditCatalogParameters catalogParameters)
    {
        return ShowAsync(new EditPasswordCatalogDialog(catalogParameters));
    }

    public Task ShowPasswordDetailsAsync(PasswordDetailsParameters passwordDetailsParameters)
    {
        return ShowAsync(new PasswordDetailsDialog(passwordDetailsParameters));
    }

    public Task ShowPasswordChangeDialogAsync(EmptyDialogParameter dialogParameter)
    {
        return ShowAsync(new ChangePasswordDialog(dialogParameter));
    }

    public Task ShowDataExportDialogAsync(ExportDataParameters dialogParameter)
    {
        return ShowAsync(new ExportDialog(dialogParameter));
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
            XamlRoot = App.Current.CurrentWindow!.Content.XamlRoot,
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
            XamlRoot = App.Current.CurrentWindow!.Content.XamlRoot,
            Style = Microsoft.UI.Xaml.Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = dialogContent.Title,
            PrimaryButtonText = string.IsNullOrEmpty(dialogContent.PrimaryButtonText) ? viewResourceLoader.GetString("Save") : dialogContent.PrimaryButtonText,
            CloseButtonText = string.IsNullOrEmpty(dialogContent.CancelButtonText) ? viewResourceLoader.GetString("Cancel") : dialogContent.CancelButtonText,
            PrimaryButtonCommand = new RelayCommand(async () => await dialogContent.ViewModel!.OnSaveAsync()),
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,

            // initially define the primary button availability
            IsPrimaryButtonEnabled = dialogContent.ViewModel!.CanSave()
        };

        // register a handler for any change of the dialog content
        dialogContent.ViewModel.DialogContext.OnContentChanged += DialogContext_OnContentChanged;
        void DialogContext_OnContentChanged(object? sender, EventArgs e)
        {
            dialog.IsPrimaryButtonEnabled = dialogContent.ViewModel.CanSave();
        }

        if (dialogContent.ViewModel is ViewModelBase viewModelBase) 
        {
            await viewModelBase.OnNavigatedToAsync(dialogContent.GetDialogParameter());
        }

        dialog.Opened += dialogContent.DialogOpened;

        _ = await dialog.ShowAsync();
    }
}
