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
        NavigationCacheMode = NavigationCacheMode.Required;

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
            DispatcherQueue.TryEnqueue(() =>
            {
                PasswordsIndex_Pivot.SelectedIndex = m.Value;
            });
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
        {
            viewModel.EditPasswordCommand.Execute(item);
        }
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
        {
            viewModel.SeePasswordCommand.Execute(item);
        }
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
        {
            viewModel.DeleteCommand.Execute(item);
        }
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
        if (DataContext is PasswordsViewModel viewModel &&
            ((MenuFlyoutItem)e.OriginalSource).DataContext is PangoExplorerItem item)
        {
            viewModel.CopyPasswordToClipboardCommand.Execute(item);
        }
    }

    /// <summary>
    /// Handles single tap on a password item to explicitly set the SelectedItem.
    /// </summary>
    private void Password_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PangoExplorerItem item } &&
            DataContext is PasswordsViewModel viewModel)
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
        if (e.OriginalSource is FrameworkElement { DataContext: PangoExplorerItem item } &&
            DataContext is PasswordsViewModel viewModel)
        {
            await viewModel.ShowPasswordDetailsAsync(item);
        }
    }

    /// <summary>
    /// Handles completion of drag-and-drop operation in the tree view with validation and deferred UI update.
    /// </summary>
    /// <param name="sender">The source TreeView control.</param>
    /// <param name="args">Event data containing dragged items and new parent.</param>
    private async void PasswordsTreeView_DragItemsCompletedAsync(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(sender);
        Log.Logger?.Debug("Drag-and-drop completed: {ItemCount} item(s)", args.Items.Count);

        if (DataContext is not PasswordsViewModel viewModel || args.Items.Count == 0)
            return;

        if (args.Items[0] is not PangoExplorerItem item)
            return;

        var newParent = args.NewParentItem as PangoExplorerItem;

        // Prevent cyclical moves: item cannot be dropped into itself or its descendants
        if (newParent != null && IsDescendantOrSelf(item, newParent))
        {
            Log.Logger?.Warning("Invalid drop: target is descendant of dragged item");
            viewModel.UpdateListCommand.Execute(null);
            return;
        }

        // If dropped on a file, use its parent folder instead
        if (newParent?.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            newParent = newParent.Parent;
        }

        // Validate folder name uniqueness when moving/creating folders
        if (item.IsFolder && newParent != null)
        {
            var siblings = newParent?.Children ?? viewModel.Passwords;
            bool folderExists = siblings.Any(s =>
                s.IsFolder &&
                s.Id != item.Id &&
                s.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));

            if (folderExists)
            {
                Log.Logger?.Warning("Folder with name '{FolderName}' already exists in target location", item.Name);
                viewModel.UpdateListCommand.Execute(null);
                return;
            }
        }

        // Defer UI update to prevent TreeView corruption during reordering
        DispatcherQueue.TryEnqueue(async () =>
        {
            await Task.Delay(150);
            await viewModel.CommitPasswordMovementAsync(item, newParent);
            Log.Logger?.Information("Password item '{ItemName}' moved successfully", item.Name);
        });
    }

    /// <summary>
    /// Checks if targetParent is a descendant of draggedItem to prevent cyclical moves.
    /// </summary>
    /// <param name="draggedItem">The item being dragged.</param>
    /// <param name="targetParent">The potential new parent item.</param>
    /// <returns>True if targetParent is the draggedItem or its descendant; otherwise, false.</returns>
    private static bool IsDescendantOrSelf(PangoExplorerItem draggedItem, PangoExplorerItem targetParent)
    {
        if (draggedItem.Id == targetParent.Id)
            return true;

        var current = targetParent.Parent;
        while (current != null)
        {
            if (current.Id == draggedItem.Id)
                return true;
            current = current.Parent;
        }
        return false;
    }

    #endregion
}
