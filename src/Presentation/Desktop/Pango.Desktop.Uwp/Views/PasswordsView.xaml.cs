using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Extensions;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.PasswordsIndex)]
public sealed partial class PasswordsView : PageBase
{
    public PasswordsView()
        : base(App.Host.Services.GetRequiredService<ILogger<PasswordsView>>())
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;

        DataContext = App.Host.Services.GetRequiredService<PasswordsViewModel>();
        PasswordsTreeView.ItemInvoked += PasswordsTreeView_ItemInvoked;
    }

    #region Overrides

    protected override void RegisterMessages()
    {
        base.RegisterMessages();
        WeakReferenceMessenger.Default.Register<NavigationRequstedMessage>(this, OnNavigationRequested);
    }

    protected override void UnregisterMessages()
    {
        base.UnregisterMessages();
        WeakReferenceMessenger.Default.Unregister<NavigationRequstedMessage>(this);
    }

    #endregion

    #region Event Handlers

    private void PasswordsTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        viewModel?.SelectedItem = args.InvokedItem as PangoExplorerItem;
    }

    private void OnNavigationRequested(object recipient, NavigationRequstedMessage message)
    {
        Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        dispatcherQueue.TryEnqueue(() =>
        {
            switch (message.Value.NavigatedView)
            {
                case AppView.EditPassword:
                    PasswordsIndex_Pivot.SelectedIndex = 1;
                    break;
                case AppView.PasswordsIndex:
                    PasswordsIndex_Pivot.SelectedIndex = 0;
                    break;
            }
        });
    }

    private void EditContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        viewModel?.EditPasswordCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem);
    }

    private void SeeContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        viewModel?.SeePasswordCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem);
    }


    private void DeleteContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        viewModel?.DeleteCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem);
    }

    private void AddPassword_CatalogContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        if (viewModel is not null)
        {
            viewModel.SelectedItem = ((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem;
            viewModel.CreatePasswordCommand.Execute(null);
        }
    }

    private void AddCatalog_CatalogContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        if (viewModel is not null)
        {
            viewModel.SelectedItem = ((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem;
            viewModel.CreateCatalogCommand.Execute(null);
        }
    }

    private void CopyPassword_PasswordContextMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        viewModel?.CopyPasswordToClipboardCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem);
    }

    private async void Password_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: PangoExplorerItem item })
        {
            await ((PasswordsViewModel)DataContext).ShowPasswordDetailsAsync(item);
        }
    }

    /// <summary>
    /// Handles the completion of a drag-and-drop operation in the tree view.
    /// Includes validation for folder name collisions and defers the UI update to prevent TreeView corruption.
    /// </summary>
    private async void PasswordsTreeView_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        System.ArgumentNullException.ThrowIfNull(sender);
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        PangoExplorerItem? item = args.Items.Count > 0 ? args.Items[0] as PangoExplorerItem : null;

        if (viewModel is not null && item is not null)
        {
            PangoExplorerItem? newParent = args.NewParentItem as PangoExplorerItem;

            if (newParent != null && IsDescendantOrSelf(item, newParent))
            {
                viewModel.UpdateListCommand.Execute(null);
                return;
            }

            if (newParent?.Type == PangoExplorerItem.ExplorerItemType.File)
            {
                newParent = newParent.Parent;
            }

            if (item.IsFolder)
            {
                var siblings = newParent == null ? viewModel.Passwords : newParent.Children;
                bool folderExists = siblings.Any(s => s.IsFolder && s.Id != item.Id && s.Name.Equals(item.Name, System.StringComparison.OrdinalIgnoreCase));

                if (folderExists)
                {
                    viewModel.UpdateListCommand.Execute(null);
                    return;
                }
            }

            DispatcherQueue.TryEnqueue(async () =>
            {
                await viewModel.CommitPasswordMovementAsync(item, newParent);
            });
        }
    }

    /// <summary>
    /// Checks if targetParent is a descendant of draggedItem to prevent cyclical moves.
    /// </summary>
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
