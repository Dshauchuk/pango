using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using Serilog;

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// View for managing passwords and catalogs in a tree view structure.
/// </summary>
[AppView(AppView.PasswordsIndex)]
public sealed partial class PasswordsView : PageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordsView"/> class.
    /// </summary>
    public PasswordsView()
            : base(App.Host.Services.GetRequiredService<ILogger<PasswordsView>>())
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;

        DataContext = App.Host.Services.GetRequiredService<PasswordsViewModel>();
        PasswordsTreeView.ItemInvoked += PasswordsTreeView_ItemInvoked;
    }

    #region Overrides

    /// <summary>
    /// Registers message subscriptions for navigation requests.
    /// </summary>
    protected override void RegisterMessages()
    {
        base.RegisterMessages();
        WeakReferenceMessenger.Default.Register<NavigationRequestedMessage>(this, OnNavigationRequested);
        WeakReferenceMessenger.Default.Register<SwitchPasswordTabMessage>(this, (r, m) =>
        {
            PasswordsIndex_Pivot.SelectedIndex = m.Value;
        });
    }

    /// <summary>
    /// Unregisters message subscriptions and event handlers to prevent memory leaks.
    /// </summary>
    protected override void UnregisterMessages()
    {
        base.UnregisterMessages();
        WeakReferenceMessenger.Default.Unregister<NavigationRequestedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SwitchPasswordTabMessage>(this);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles tree view item invocation: updates the selected item in the view model.
    /// </summary>
    /// <param name="sender">The source TreeView control.</param>
    /// <param name="args">Event data containing the invoked item.</param>
    private void PasswordsTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (DataContext is PasswordsViewModel viewModel)
        {
            viewModel.SelectedItem = args.InvokedItem as PangoExplorerItem;
        }
    }

    /// <summary>
    /// Handles navigation requests to switch between index and edit views via pivot selection.
    /// </summary>
    /// <param name="recipient">The message recipient instance.</param>
    /// <param name="message">The navigation request message.</param>
    private void OnNavigationRequested(object recipient, NavigationRequestedMessage message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (message.Value.NavigatedView == AppView.EditPassword)
            {
                PasswordsIndex_Pivot.SelectedIndex = 1;
            }
            else if (message.Value.NavigatedView == AppView.PasswordsIndex)
            {
                PasswordsIndex_Pivot.SelectedIndex = 0;
            }
        });
    }

    /// <summary>
    /// Handles Edit command from context menu: executes view model's edit password command.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data.</param>
    private void EditContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PasswordsViewModel viewModel &&
            ((MenuFlyoutItem)e.OriginalSource).DataContext is PangoExplorerItem item)
            viewModel.EditPasswordCommand.Execute(item);
    }

    /// <summary>
    /// Handles See command from context menu: executes view model's view password command.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data.</param>
    private void SeeContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PasswordsViewModel viewModel && 
            ((MenuFlyoutItem)e.OriginalSource).DataContext is PangoExplorerItem item)
            viewModel.SeePasswordCommand.Execute(item);
    }

    /// <summary>
    /// Handles Delete command from context menu: executes view model's delete command.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data.</param>
    private void DeleteContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PasswordsViewModel viewModel && 
            ((MenuFlyoutItem)e.OriginalSource).DataContext is PangoExplorerItem item)
            viewModel.DeleteCommand.Execute(item);
    }

    /// <summary>
    /// Handles Add Password command from catalog context menu.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data.</param>
    private void AddPassword_CatalogContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PasswordsViewModel viewModel && 
            ((MenuFlyoutItem)e.OriginalSource).DataContext is PangoExplorerItem item)
        {
            viewModel.SelectedItem = item;
            viewModel.CreatePasswordCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles Add Catalog command from catalog context menu.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data.</param>
    private void AddCatalog_CatalogContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PasswordsViewModel viewModel &&
            ((MenuFlyoutItem)e.OriginalSource).DataContext is PangoExplorerItem item)
        {
            viewModel.SelectedItem = item;
            viewModel.CreateCatalogCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handles Copy Password command from password context menu.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data.</param>
    private void CopyPassword_PasswordContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PasswordsViewModel viewModel && ((MenuFlyoutItem)e.OriginalSource).DataContext is PangoExplorerItem item)
            viewModel.CopyPasswordToClipboardCommand.Execute(item);
    }

    private void ToggleStarButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.DataContext is PangoExplorerItem item && DataContext is PasswordsViewModel viewModel)
            {
                viewModel.ToggleStarCommand.Execute(item);
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.Error(ex, "ToggleStarButton_Click crashed");
        }
    }

    private void CopyPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.DataContext is PangoExplorerItem item && DataContext is PasswordsViewModel viewModel)
            {
                viewModel.CopyPasswordToClipboardCommand.Execute(item);
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.Error(ex, "CopyPasswordButton_Click crashed");
        }
    }

    private void SeePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.DataContext is PangoExplorerItem item && DataContext is PasswordsViewModel viewModel)
            {
                viewModel.SeePasswordCommand.Execute(item);
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.Error(ex, "SeePasswordButton_Click crashed");
        }
    }

    /// <summary>
    /// Handles single tap on a password item to explicitly set the SelectedItem.
    /// </summary>
    private void Password_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PangoExplorerItem item } && DataContext is PasswordsViewModel viewModel)
        {
            viewModel.SelectedItem = item;
        }
    }

    /// <summary>
    /// Handles double-tap on password item: shows password details asynchronously.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Double-tap event arguments.</param>
    private async void Password_DoubleTappedAsync(object _, DoubleTappedRoutedEventArgs e)
    {
        try
        {
            if (e.OriginalSource is FrameworkElement { DataContext: PangoExplorerItem item } && DataContext is PasswordsViewModel viewModel)
            {
                await viewModel.ShowPasswordDetailsAsync(item);
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.Error(ex, "Exception in Password_DoubleTappedAsync");
        }
    }

    /// <summary>
    /// Handles completion of drag-and-drop operation in the tree view with validation and deferred UI update.
    /// </summary>
    /// <param name="sender">The source TreeView control.</param>
    /// <param name="args">Event data containing dragged items and new parent.</param>
    private void PasswordsTreeView_DragItemsCompletedAsync(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        try
        {
            if (args.Items.Count == 0 || DataContext is not PasswordsViewModel viewModel) return;
            if (args.Items[0] is not PangoExplorerItem item) return;

            var newParent = args.NewParentItem as PangoExplorerItem;

            if (newParent != null && IsDescendantOrSelf(item, newParent))
            {
                viewModel.UpdateListCommand.Execute(null);
                return;
            }

            if (newParent?.Type == PangoExplorerItem.ExplorerItemType.File)
            {
                var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
                WeakReferenceMessenger.Default.Send(
                    new Mvvm.Models.InAppNotificationMessage(
                        resourceLoader.GetString("CannotDropIntoPassword") ?? "Cannot drop items into a password",
                        AppNotificationType.Warning));

                viewModel.UpdateListCommand.Execute(null);
                return;
            }

            if (newParent?.Id == item.Parent?.Id)
            {
                viewModel.UpdateListCommand.Execute(null);
                return;
            }

            var siblings = newParent == null ? viewModel.Passwords : newParent.Children;
            if (siblings != null && siblings.Any(c => c.Type == item.Type && c.Id != item.Id && string.Equals(c.Name, item.Name, StringComparison.OrdinalIgnoreCase)))
            {
                var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
                WeakReferenceMessenger.Default.Send(
                    new Mvvm.Models.InAppNotificationMessage(
                        resourceLoader.GetString("ValidationError_CatalogExists") ?? "Item already exists",
                        AppNotificationType.Warning));

                viewModel.UpdateListCommand.Execute(null);
                return;
            }

            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await Task.Delay(150);
                    await viewModel.CommitPasswordMovementAsync(item, newParent);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Drag and drop commit failed");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Logger?.Error(ex, "DragItemsCompletedAsync handler failed");
        }
    }

    /// <summary>
    /// Checks if targetParent is a descendant of draggedItem to prevent cyclical moves.
    /// </summary>
    /// <param name="draggedItem">The item being dragged.</param>
    /// <param name="targetParent">The potential new parent item.</param>
    /// <returns>True if targetParent is the draggedItem or its descendant; otherwise, false.</returns>
    private static bool IsDescendantOrSelf(PangoExplorerItem draggedItem, PangoExplorerItem targetParent)
    {
        if (draggedItem.Id == targetParent.Id) return true;
        var current = targetParent.Parent;
        while (current != null)
        {
            if (current.Id == draggedItem.Id) return true;
            current = current.Parent;
        }
        return false;
    }

    #endregion
}
