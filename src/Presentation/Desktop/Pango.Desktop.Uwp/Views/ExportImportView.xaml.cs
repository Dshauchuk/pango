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
using Serilog;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// View for handling export and import operations of password data.
/// </summary>
[AppView(Core.Enums.AppView.ExportImport)]
public sealed partial class ExportImportView : PageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportImportView"/> class.
    /// </summary>
    public ExportImportView()
        : base(App.Host.Services.GetRequiredService<ILogger<ExportImportView>>())
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;
        DataContext = App.Host.Services.GetRequiredService<ExportImportViewModel>();

        Log.Logger?.Debug("ExportImportView initialized");
    }

    /// <summary>
    /// Called when the page becomes the current frame content: registers event handlers and message subscriptions.
    /// </summary>
    /// <param name="e">Event data for the navigation event.</param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Log.Logger?.Debug("ExportImportView navigated to");

        PasswordsTreeView.ItemInvoked += OnTreeViewItemInvoked;
        ImportTreeView.ItemInvoked += OnTreeViewItemInvoked;

        // Safe registration with UI thread dispatch and error handling
        WeakReferenceMessenger.Default.Register<ImportPreviewReadyMessage>(this, (r, m) =>
        {
            if (XamlRoot == null)
            {
                Log.Logger?.Warning("ImportPreviewReadyMessage received but XamlRoot is null");
                return;
            }

            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    if (ViewModel is ExportImportViewModel vm)
                    {
                        await vm.HandleImportPreviewAsync(m.Value);
                        Log.Logger?.Information("Import preview handled successfully");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to handle import preview.");
                }
            });
        });
    }

    /// <summary>
    /// Called when the page is no longer the current frame content: cleans up event handlers and message subscriptions.
    /// </summary>
    /// <param name="e">Event data for the navigation event.</param>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Log.Logger?.Debug("ExportImportView navigated from: cleaning up resources");

        base.OnNavigatedFrom(e);
        WeakReferenceMessenger.Default.UnregisterAll(this);
        PasswordsTreeView.ItemInvoked -= OnTreeViewItemInvoked;
        ImportTreeView.ItemInvoked -= OnTreeViewItemInvoked;
    }

    /// <summary>
    /// Handles TreeView item invocation: toggles selection state and notifies command state changes.
    /// </summary>
    /// <param name="sender">The TreeView that raised the event.</param>
    /// <param name="args">Event data containing the invoked item.</param>
    private void OnTreeViewItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is not PangoExplorerItem item)
            return;

        item.IsSelected = !item.IsSelected;
        Log.Logger?.Debug("TreeView item '{ItemName}' selection toggled: {IsSelected}", item.Name, item.IsSelected);

        if (ViewModel is ExportImportViewModel vm)
        {
            if (sender == PasswordsTreeView)
            {
                vm.ExportDataCommand.NotifyCanExecuteChanged();
                Log.Logger?.Debug("ExportDataCommand state refreshed");
            }
            else if (sender == ImportTreeView)
            {
                vm.FinalizeImportCommand.NotifyCanExecuteChanged();
                Log.Logger?.Debug("FinalizeImportCommand state refreshed");
            }
        }
    }

    /// <summary>
    /// Handles file picker button click: allows user to select a .pngx file for import asynchronously.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private async void PickPngxFileButton_ClickAsync(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Log.Logger?.Debug("PickPngxFileButton clicked");

        if (ViewModel is not ExportImportViewModel viewModel)
        {
            Log.Logger?.Warning("ViewModel is not ExportImportViewModel");
            return;
        }

        try
        {
            var openPicker = new FileOpenPicker();
            var window = App.Current.CurrentWindow;

            if (window == null)
            {
                Log.Logger?.Error("Current window is null, cannot initialize file picker");
                return;
            }

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            openPicker.FileTypeFilter.Add(".pngx");

            var file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                Log.Logger?.Information("File selected for import: {FileName}", file.Name);

                var tempFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;
                var copiedFile = await file.CopyAsync(
                    tempFolder,
                    file.Name,
                    Windows.Storage.NameCollisionOption.ReplaceExisting);

                DispatcherQueue.TryEnqueue(() => viewModel.ImportFilePath = copiedFile.Path);
                Log.Logger?.Information("File copied to cache and path assigned to ViewModel");
            }
            else
            {
                Log.Logger?.Debug("File picker cancelled by user");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error picking file for import");
        }
    }
}
