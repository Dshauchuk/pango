using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.Views;
using Pango.Desktop.Uwp.ViewModels;
using Serilog;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.Dialogs;

/// <summary>
/// Centralized service for displaying modal dialogs with consistent styling and lifecycle management.
/// </summary>
public class DialogService : IDialogService
{
    /// <summary>
    /// Shows the new catalog creation/edit dialog asynchronously.
    /// </summary>
    /// <param name="catalogParameters">Parameters containing catalog data and context.</param>
    public Task ShowNewCatalogDialogAsync(EditCatalogParameters catalogParameters)
    {
        Log.Logger?.Debug("Showing new catalog dialog");
        return ShowAsync(new EditPasswordCatalogDialog(catalogParameters));
    }

    /// <summary>
    /// Shows the password details view dialog asynchronously.
    /// </summary>
    /// <param name="passwordDetailsParameters">Parameters containing password ID and available catalogs.</param>
    public Task ShowPasswordDetailsAsync(PasswordDetailsParameters passwordDetailsParameters)
    {
        Log.Logger?.Debug("Showing password details dialog");
        return ShowAsync(new PasswordDetailsDialog(passwordDetailsParameters));
    }

    /// <summary>
    /// Shows the password change dialog asynchronously.
    /// </summary>
    /// <param name="dialogParameter">Empty dialog parameter for context.</param>
    public Task ShowPasswordChangeDialogAsync(EmptyDialogParameter dialogParameter)
    {
        Log.Logger?.Debug("Showing password change dialog");
        return ShowAsync(new ChangePasswordDialog(dialogParameter));
    }

    /// <summary>
    /// Shows the data export configuration dialog asynchronously.
    /// </summary>
    /// <param name="dialogParameter">Parameters containing items selected for export.</param>
    public Task ShowDataExportDialogAsync(ExportDataParameters dialogParameter)
    {
        Log.Logger?.Debug("Showing data export dialog");
        return ShowAsync(new ExportDialog(dialogParameter));
    }

    /// <summary>
    /// Shows the data import file selection dialog asynchronously.
    /// </summary>
    /// <param name="dialogParameter">Parameters containing the import file path.</param>
    public Task ShowDataImportDialogAsync(ImportDataParameters dialogParameter)
    {
        Log.Logger?.Debug("Showing data import dialog for file: {FilePath}", dialogParameter.FilePath);
        return ShowAsync(new ImportDialog(dialogParameter));
    }

    /// <summary>
    /// Shows the export completion result dialog asynchronously.
    /// </summary>
    /// <param name="dialogParameter">Parameters containing export result summary.</param>
    public Task ShowExportResultDialogAsync(ExportResultParameters dialogParameter)
    {
        Log.Logger?.Debug("Showing export result dialog");
        return ShowAsync(new ExportCompletedDialog(dialogParameter));
    }

    /// <summary>
    /// Shows the password generation configuration dialog asynchronously.
    /// </summary>
    /// <param name="dialogParameter">Parameters containing initial password value and options.</param>
    public Task ShowGeneratePasswordDialogAsync(GeneratePasswordDialogParameters dialogParameter)
    {
        Log.Logger?.Debug("Showing generate password dialog");
        return ShowAsync(new GeneratePasswordDialog(dialogParameter));
    }

    /// <summary>
    /// Shows a simple confirmation dialog and returns the user's choice asynchronously.
    /// </summary>
    /// <param name="confirmationTitle">The title text displayed in the dialog.</param>
    /// <param name="confirmationText">The body message text displayed in the dialog.</param>
    /// <returns>True if the user clicked the primary (Yes) button; false otherwise.</returns>
    public async Task<bool> ConfirmAsync(string confirmationTitle, string confirmationText)
    {
        Log.Logger?.Debug("Showing confirmation dialog: {Title}", confirmationTitle);

        var resourceLoader = new ResourceLoader();
        var rootElement = App.Current.CurrentWindow?.Content as FrameworkElement;

        var confirmDialog = new ContentDialog
        {
            XamlRoot = App.Current.CurrentWindow!.Content.XamlRoot,
            RequestedTheme = rootElement?.RequestedTheme ?? ElementTheme.Default,
            Title = confirmationTitle,
            Content = confirmationText,
            CloseButtonText = resourceLoader.GetString("Cancel"),
            PrimaryButtonText = resourceLoader.GetString("Yes"),
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await confirmDialog.ShowAsync();
        var accepted = result == ContentDialogResult.Primary;

        Log.Logger?.Debug("Confirmation dialog result: {Accepted}", accepted);
        return accepted;
    }

    /// <summary>
    /// Internal helper method that configures and displays a content dialog with consistent styling and event handling.
    /// </summary>
    /// <param name="dialogContent">The dialog content implementing IContentDialog with ViewModel.</param>
    private async Task ShowAsync(IContentDialog dialogContent)
    {
        Log.Logger?.Debug("Showing dialog: {DialogType}", dialogContent.GetType().Name);

        var resourceLoader = new ResourceLoader();
        var rootElement = App.Current.CurrentWindow?.Content as FrameworkElement;

        var contentDialog = new ContentDialog
        {
            XamlRoot = App.Current.CurrentWindow!.Content.XamlRoot,
            RequestedTheme = rootElement?.RequestedTheme ?? ElementTheme.Default,
            Style = Microsoft.UI.Xaml.Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = dialogContent.Title,
            PrimaryButtonText = string.IsNullOrEmpty(dialogContent.PrimaryButtonText)
                ? resourceLoader.GetString("Save")
                : dialogContent.PrimaryButtonText,
            CloseButtonText = string.IsNullOrEmpty(dialogContent.CancelButtonText)
                ? resourceLoader.GetString("Cancel")
                : dialogContent.CancelButtonText,
            DefaultButton = ContentDialogButton.Primary,
            Content = dialogContent,
            IsPrimaryButtonEnabled = dialogContent.ViewModel?.CanSave() ?? false
        };

        contentDialog.PrimaryButtonClick += async (s, e) =>
        {
            Log.Logger?.Debug("Dialog primary button clicked");

            var deferral = e.GetDeferral();
            try
            {
                if (dialogContent.ViewModel != null)
                {
                    await dialogContent.ViewModel.OnSaveAsync();
                    if (!dialogContent.ViewModel.CanSave())
                    {
                        Log.Logger?.Debug("Save validation failed, keeping dialog open");
                        e.Cancel = true;
                    }
                    else
                    {
                        Log.Logger?.Information("Dialog save completed successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Error during dialog save operation");
                e.Cancel = true;
            }
            finally
            {
                deferral.Complete();
            }
        };

        contentDialog.CloseButtonClick += async (s, e) =>
        {
            Log.Logger?.Debug("Dialog close/cancel button clicked");

            var deferral = e.GetDeferral();
            try
            {
                if (dialogContent.ViewModel != null)
                {
                    await dialogContent.ViewModel.OnCancelAsync();
                    Log.Logger?.Debug("Dialog cancel completed");
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Error during dialog cancel operation");
            }
            finally
            {
                deferral.Complete();
            }
        };

        // Subscribe to content changes to update button state dynamically
        if (dialogContent.ViewModel?.DialogContext != null)
        {
            dialogContent.ViewModel.DialogContext.OnContentChanged += DialogContext_OnContentChanged;
        }

        void DialogContext_OnContentChanged(object? sender, EventArgs eventArgs)
        {
            contentDialog.IsPrimaryButtonEnabled = dialogContent.ViewModel?.CanSave() ?? false;
            Log.Logger?.Debug("Dialog content changed: primary button enabled = {Enabled}", contentDialog.IsPrimaryButtonEnabled);
        }

        // Initialize ViewModel if it implements navigation interface
        if (dialogContent.ViewModel is ViewModelBase viewModelBase)
        {
            await viewModelBase.OnNavigatedToAsync(dialogContent.GetDialogParameter());
            Log.Logger?.Debug("ViewModel OnNavigatedToAsync completed");
        }

        contentDialog.Opened += dialogContent.DialogOpened;

        try
        {
            // Wait for the dialog to close
            await contentDialog.ShowAsync();
            Log.Logger?.Debug("Dialog closed");
        }
        finally
        {
            // Clean up event subscriptions to prevent memory leaks
            if (dialogContent.ViewModel?.DialogContext != null)
            {
                dialogContent.ViewModel.DialogContext.OnContentChanged -= DialogContext_OnContentChanged;
            }
            contentDialog.Opened -= dialogContent.DialogOpened;
            Log.Logger?.Debug("Dialog event handlers unsubscribed");
        }
    }
}
