using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.Views;
using Pango.Desktop.Uwp.ViewModels;
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

    public Task ShowDataImportDialogAsync(ImportDataParameters dialogParameter)
    {
        return ShowAsync(new ImportDialog(dialogParameter));
    }

    public Task ShowExportResultDialogAsync(ExportResultParameters dialogParameter)
    {
        return ShowAsync(new ExportCompletedDialog(dialogParameter));
    }

    public Task ShowGeneratePasswordDialogAsync(GeneratePasswordDialogParameters dialogParameter)
    {
        return ShowAsync(new GeneratePasswordDialog(dialogParameter));
    }

    /// <summary>
    /// Raises a simple confirmation dialog, returns true if user clicked on the primary button, otherwise - false
    /// </summary>
    /// <param name="confirmationTitle">The title of the dialog</param>
    /// <param name="confirmationText">The body text of the dialog</param>
    /// <returns>True if accepted, False otherwise</returns>
    public async Task<bool> ConfirmAsync(string confirmationTitle, string confirmationText)
    {
        ResourceLoader viewResourceLoader = new();

        var rootElement = App.Current.CurrentWindow?.Content as FrameworkElement;

        ContentDialog subscribeDialog = new()
        {
            XamlRoot = App.Current.CurrentWindow!.Content.XamlRoot,
            RequestedTheme = rootElement?.RequestedTheme ?? ElementTheme.Default,
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
        ResourceLoader viewResourceLoader = new();

        var rootElement = App.Current.CurrentWindow?.Content as FrameworkElement;

        ContentDialog contentDialog = new()
        {
            XamlRoot = App.Current.CurrentWindow!.Content.XamlRoot,
            RequestedTheme = rootElement?.RequestedTheme ?? ElementTheme.Default,
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

        void DialogContext_OnContentChanged(object? sender, EventArgs eventArgs)
        {
            contentDialog.IsPrimaryButtonEnabled = dialogContent.ViewModel.CanSave();
        }

        if (dialogContent.ViewModel is ViewModelBase viewModelBase)
        {
            await viewModelBase.OnNavigatedToAsync(dialogContent.GetDialogParameter());
        }

        contentDialog.Opened += dialogContent.DialogOpened;

        // Wait for the dialog to close
        await contentDialog.ShowAsync();

        // Clean up event subscriptions to allow Garbage Collection
        dialogContent.ViewModel.DialogContext.OnContentChanged -= DialogContext_OnContentChanged;
        contentDialog.Opened -= dialogContent.DialogOpened;
    }
}
