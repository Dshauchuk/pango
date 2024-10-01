using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System;
using System.Linq;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[AppView(Core.Enums.AppView.ExportImport)]
public sealed partial class ExportImportView : PageBase
{
    public ExportImportView()
    : base(App.Host.Services.GetRequiredService<ILogger<ExportImportView>>())
    {
        this.InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<ExportImportViewModel>();

        this.PasswordsTreeView.SelectionChanged += PasswordsTreeView_SelectionChanged;
    }

    private void PasswordsTreeView_SelectionChanged(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.Any())
        {
            foreach (var item in args.AddedItems)
            {
                if (item is PangoExplorerItem explorerItem)
                {
                    if (!explorerItem.IsSelected)
                    {
                        explorerItem.IsSelected = true;

                        if(explorerItem.Parent != null)
                        {
                            explorerItem.Parent.IsSelected = true;
                        }
                    }
                }
            }
        }

        if (args.RemovedItems.Any())
        {
            foreach (var item in args.RemovedItems)
            {
                if (item is PangoExplorerItem explorerItem)
                {
                    explorerItem.IsSelected = false;
                }
            }
        }
    }

    private async void PickPngxFileButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel is not ExportImportViewModel viewModel)
        {
            return;
        }

        viewModel.ImportFilePath = string.Empty;

        // Create a file picker
        var openPicker = new Windows.Storage.Pickers.FileOpenPicker();

        // See the sample code below for how to make the window accessible from the App class.
        var window = App.Current.CurrentWindow;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Initialize the file picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your file picker
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.SuggestedStartLocation = PickerLocationId.Downloads;
        openPicker.FileTypeFilter.Add(".pngx");

        // Open the picker for the user to pick a file
        var file = await openPicker.PickSingleFileAsync();
        if (file != null)
        {
            viewModel.ImportFilePath = file.Path;
        }
        else
        {
            viewModel.ImportFilePath = string.Empty;
        }
    }
}
