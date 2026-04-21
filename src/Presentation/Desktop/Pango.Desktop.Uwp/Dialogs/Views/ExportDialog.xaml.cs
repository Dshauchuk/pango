// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.Extensions.DependencyInjection;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.ViewModels;

namespace Pango.Desktop.Uwp.Dialogs.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ExportDialog : DialogPage
{
    // Constructor: Initializes the export dialog with parameters and sets up the view model
    public ExportDialog(ExportDataParameters parameter) : base(parameter)
    {
        InitializeComponent();
        SetViewModel(App.Host.Services.GetRequiredService<ExportDialogViewModel>());
    }

    // Gets the title for the export dialog from resources
    public override string Title => ViewResourceLoader.GetString("ExportTitle");

    // Gets the primary button text for exporting data from resources
    public override string? PrimaryButtonText => ViewResourceLoader.GetString("ExportDataButtonContent");

    // Handles folder selection button click to allow user to choose export path
    private async void SelectFolderButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var window = App.Current.CurrentWindow;
        if (window == null)
            return;

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        var folderPicker = new Windows.Storage.Pickers.FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        folderPicker.FileTypeFilter.Add("*");

        Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            if (DataContext is ExportDialogViewModel viewModel)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    viewModel.ExportFolderPath = folder.Path;
                    viewModel.Validator.Validate();
                    viewModel.DialogContext.RaiseDialogContentChanged();
                });
            }
        }
    }
}
