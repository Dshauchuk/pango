using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.PasswordsIndex)]
public sealed partial class PasswordsView : PageBase
{
    private IEnumerable<PasswordExplorerItem> _passwordsTreeBeforeDragAndDrop;

    public PasswordsView()
        : base(App.Host.Services.GetRequiredService<ILogger<PasswordsView>>())
    {
        this.InitializeComponent();

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

    private void PasswordsTreeView_ItemInvoked(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        if (viewModel is not null)
        {
            viewModel.SelectedItem = args.InvokedItem as PasswordExplorerItem;
        }
    }

    private void OnNavigationRequested(object recipient, NavigationRequstedMessage message)
    {
        Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        dispatcherQueue.TryEnqueue(() =>
        {
            switch (message.Value.NavigatedView)
            {
                case Core.Enums.AppView.EditPassword:
                    PasswordsIndex_Pivot.SelectedIndex = 1;
                    break;
                case Core.Enums.AppView.PasswordsIndex:
                    PasswordsIndex_Pivot.SelectedIndex = 0;
                    break;
            }
        });
    }

    private void EditContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        viewModel?.EditPasswordCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem);
    }

    private void SeeContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        viewModel?.SeePasswordCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem);
    }


    private void DeleteContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        viewModel?.DeleteCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem);
    }

    private void AddPassword_CatalogContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        if(viewModel is not null)
        {
            viewModel.SelectedItem = ((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem;
            viewModel.CreatePasswordCommand.Execute(null);
        }
    }

    private void AddCatalog_CatalogContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        
        if(viewModel is not null)
        {
            viewModel.SelectedItem = ((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem;
            viewModel.CreateCatalogCommand.Execute(null);
        }
    }

    private void CopyPassword_PasswordContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        if(viewModel is not null)
        {
            viewModel.CopyPasswordToClipboardCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem);
        }
    }

    private async void Password_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        var item = (e.OriginalSource as FrameworkElement).DataContext as PasswordExplorerItem;

        await ((PasswordsViewModel)DataContext).ShowPasswordDetailsAsync(item);
    }

    private void TreeViewItem_DropCompleted(UIElement sender, DropCompletedEventArgs args)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        PasswordExplorerItem? item = ((TreeViewItem)sender).DataContext as PasswordExplorerItem;

        if (viewModel is not null && !ValidatePasswordItemInTree(viewModel.Passwords, item))
        {
            viewModel.Passwords = new ObservableCollection<PasswordExplorerItem>(_passwordsTreeBeforeDragAndDrop);
            WeakReferenceMessenger.Default.Send(new PasswordUpdatedMessage(null));
        }
    }

    private void TreeViewItem_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        if (viewModel is not null)
        {
            _passwordsTreeBeforeDragAndDrop = new List<PasswordExplorerItem>(viewModel.Passwords);
        }
    }

    #endregion

    private bool ValidatePasswordItemInTree(IEnumerable<PasswordExplorerItem> itemsSource, PasswordExplorerItem itemToValidate)
    {
        PasswordExplorerItem? parentItem = FindParentItemInTree(itemsSource, itemToValidate.Id, null);

        // valid if Parent is null and item is on the first level of the tree
        if (parentItem is null)
        {
            return itemsSource.Any(s => s.Id == itemToValidate.Id);
        }

        return parentItem.Type != PasswordExplorerItem.ExplorerItemType.File;
    }

    private PasswordExplorerItem? FindParentItemInTree(IEnumerable<PasswordExplorerItem> itemsSource, Guid itemIdToFindParent, PasswordExplorerItem? parentItem)
    {
        if (itemsSource is null)
            return null;

        foreach (PasswordExplorerItem item in itemsSource)
        {
            if (item.Id == itemIdToFindParent)
            {
                return parentItem;
            }
            PasswordExplorerItem? foundParentItem = null;
            if (item.Children?.Any() == true)
            {
                foundParentItem = FindParentItemInTree(item.Children, itemIdToFindParent, item);
                if (foundParentItem is not null)
                {
                    return foundParentItem;
                }
            }
        }

        return null;
    }
}
