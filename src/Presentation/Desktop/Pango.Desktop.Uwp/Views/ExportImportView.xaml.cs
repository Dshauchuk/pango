using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Dialogs.ViewModels;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System;
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
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        DataContext = App.Host.Services.GetRequiredService<ExportImportViewModel>();

        // Subscribe to ItemInvoked events for checkbox toggle logic
        PasswordsTreeView.ItemInvoked += OnTreeViewItemInvoked;
        ImportTreeView.ItemInvoked += OnTreeViewItemInvoked;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Safe registration
        WeakReferenceMessenger.Default.Register<ImportPreviewReadyMessage>(this, (r, m) =>
        {
            // Check if page is still valid
            if (XamlRoot == null) return;

            // Dispatch to UI thread to avoid threading issues
            DispatcherQueue.TryEnqueue(() =>
            {
                if (ViewModel is ExportImportViewModel vm)
                {
                    _ = vm.HandleImportPreviewAsync(m.Value);
                }
            });
        });
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    // Handle TreeView item clicks to toggle checkbox selection
    private void OnTreeViewItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is PangoExplorerItem item)
        {
            // Toggle selection state
            item.IsSelected = !item.IsSelected;

            // Notify commands about selection change
            if (ViewModel is ExportImportViewModel vm)
            {
                if (sender == PasswordsTreeView)
                    vm.ExportDataCommand.NotifyCanExecuteChanged();
                else if (sender == ImportTreeView)
                    vm.FinalizeImportCommand.NotifyCanExecuteChanged();
            }
        }
    }

    // Handles picking the .pngx file
    private async void PickPngxFileButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel is not ExportImportViewModel viewModel) return;

        try
        {
            var openPicker = new FileOpenPicker();

            // Get window handle safely
            var window = App.Current.CurrentWindow;
            if (window == null) return;

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            openPicker.FileTypeFilter.Add(".pngx");

            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                viewModel.ImportFilePath = file.Path;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error picking file");
        }
    }
}
