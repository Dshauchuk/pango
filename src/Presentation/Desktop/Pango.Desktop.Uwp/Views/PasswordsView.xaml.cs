﻿using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Application.Common;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
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
    private int _passwordsTreeBeforeDragAndDropItemIndex;
    private PasswordExplorerItem? _draggedItemOldParent;

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
            viewModel.SelectedItem = args.InvokedItem as PangoExplorerItem;
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
        viewModel?.EditPasswordCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem);
    }

    private void SeeContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        viewModel?.SeePasswordCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem);
    }


    private void DeleteContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        viewModel?.DeleteCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem);
    }

    private void AddPassword_CatalogContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        if(viewModel is not null)
        {
            viewModel.SelectedItem = ((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem;
            viewModel.CreatePasswordCommand.Execute(null);
        }
    }

    private void AddCatalog_CatalogContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        
        if(viewModel is not null)
        {
            viewModel.SelectedItem = ((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem;
            viewModel.CreateCatalogCommand.Execute(null);
        }
    }

    private void CopyPassword_PasswordContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;

        if(viewModel is not null)
        {
            viewModel.CopyPasswordToClipboardCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PangoExplorerItem);
        }
    }

    private async void Password_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: PangoExplorerItem item })
        {
            await ((PasswordsViewModel)DataContext).ShowPasswordDetailsAsync(item);
        }
    }

    private void PasswordsTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        PasswordExplorerItem? item = args.Items.FirstOrDefault() as PasswordExplorerItem;

        if (viewModel is not null && item is not null)
        {
            _passwordsTreeBeforeDragAndDropItemIndex = item.Parent is null ? viewModel.Passwords.IndexOf(item) : item.Parent.Children.IndexOf(item);
            _passwordsTreeBeforeDragAndDropItemIndex = _passwordsTreeBeforeDragAndDropItemIndex == -1 ? 0 : _passwordsTreeBeforeDragAndDropItemIndex;

            _draggedItemOldParent = item.Parent;
        }
    }

    private async void PasswordsTreeView_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
    {
        PasswordsViewModel? viewModel = DataContext as PasswordsViewModel;
        PasswordExplorerItem? item = args.Items.FirstOrDefault() as PasswordExplorerItem;

        if (viewModel is not null && item is not null)
        {
            PasswordExplorerItem? newParent = args.NewParentItem as PasswordExplorerItem;
            if (newParent?.Type == PasswordExplorerItem.ExplorerItemType.File)
            {
                newParent = MoveItemToParentOfFile(item, newParent, viewModel.Passwords);
            }
            else
            {
                // for File type items are alrady ordered
                OrderByTypeAfterElementMoved(newParent?.Children ?? viewModel.Passwords, item);
            }

            SetNewParent(item, newParent);

            await viewModel.UpdatePasswordItemsInTreeAsync();
        }
    }

    #endregion

    /// <summary>
    /// Sets new parent for the passed <paramref name="item"/>
    /// </summary>
    /// <param name="item">Item, for which new parent should be set</param>
    /// <param name="newParent">New parent for the passed <paramref name="item"/></param>
    private static void SetNewParent(PasswordExplorerItem item, PasswordExplorerItem? newParent)
    {
        item.Parent = newParent;
        RecalculateCatalogPath(item);
    }

    /// <summary>
    /// Recalculates <see cref="PasswordExplorerItem.CatalogPath"/> field, based on the <see cref="PasswordExplorerItem.Parent"/> for passed <paramref name="item"/> and all its Children
    /// </summary>
    /// <param name="item">Element, for which and for which Children <see cref="PasswordExplorerItem.CatalogPath"/> should be recalculated</param>
    private static void RecalculateCatalogPath(PasswordExplorerItem item)
    {
        item.CatalogPath = item.Parent is null ? string.Empty : $"{item.Parent.CatalogPath}{AppConstants.CatalogDelimeter}{item.Parent.Name}";

        if (item.Type == PasswordExplorerItem.ExplorerItemType.Folder)
        {
            foreach (PasswordExplorerItem child in item.Children)
            {
                RecalculateCatalogPath(child);
            }
        }
    }

    /// <summary>
    /// Orders passed <paramref name="movedElement"/> within the <paramref name="passwords"/> collection
    /// </summary>
    /// <param name="passwords">List of passwords (one of them is <paramref name="movedElement"/>)</param>
    /// <param name="movedElement">Password, that was added to the <paramref name="passwords"/> collection and should be ordered within the collection</param>
    private static void OrderByTypeAfterElementMoved(ObservableCollection<PasswordExplorerItem> passwords, PasswordExplorerItem movedElement)
    {
        int currentElementIndex = passwords.IndexOf(movedElement);

        int orderedElementIndex = GetOrderedItemIndex(passwords, movedElement);

        // don't use ObservableCollection.Move, because it doesn't triggers tree to redraw
        passwords.RemoveAt(currentElementIndex);
        passwords.Insert(orderedElementIndex, movedElement);
    }

    /// <summary>
    /// Returns index of ordered <paramref name="item"/> within the passed <paramref name="passwords"/> collection
    /// </summary>
    /// <param name="passwords">List of passwords (one of them is <paramref name="item"/>)</param>
    /// <param name="item"></param>
    /// <returns>Index of ordered <paramref name="item"/> within the passed <paramref name="passwords"/> collection</returns>
    private static int GetOrderedItemIndex(IEnumerable<PasswordExplorerItem> passwords, PasswordExplorerItem item)
    {
        int orderedElementIndex = passwords.OrderByDescending(p => p.Type, Comparer<PasswordExplorerItem.ExplorerItemType>.Create((e1, e2) =>
        {
            if (e1 == PasswordExplorerItem.ExplorerItemType.Folder && e2 == PasswordExplorerItem.ExplorerItemType.File)
            {
                return 1;
            }
            if (e1 == PasswordExplorerItem.ExplorerItemType.File && e2 == PasswordExplorerItem.ExplorerItemType.Folder)
            {
                return -1;
            }
            return 0;
        })).ThenBy(ps => ps.Name).ToList().IndexOf(item);

        return orderedElementIndex == -1 ? 0 : orderedElementIndex;
    }

    /// <summary>
    /// Move passed <paramref name="item"/> from <paramref name="file"/> to <paramref name="file"/>'s Parent item. If <paramref name="file"/> doesn't have Parent - move item to the <paramref name="itemsSource"/>
    /// </summary>
    /// <param name="item">Item to move</param>
    /// <param name="file">File, from which <paramref name="item"/> should be moved</param>
    /// <param name="itemsSource">Collection of all passwords in tree format</param>
    /// <returns>New parent of a passed <paramref name="item"/></returns>
    private static PasswordExplorerItem? MoveItemToParentOfFile(PasswordExplorerItem item, PasswordExplorerItem file, ObservableCollection<PasswordExplorerItem> itemsSource)
    {
        file.Children.Remove(item);

        ObservableCollection<PasswordExplorerItem> targetCollection;
        if (file.Parent is null)
        {
            targetCollection = itemsSource;
        }
        else
        {
            targetCollection = file.Parent.Children;
        }

        int orderedElementIndex = GetOrderedItemIndex(targetCollection.Union(new PasswordExplorerItem[1] { item }), item);
        targetCollection.Insert(orderedElementIndex, item);

        return file.Parent;
    }
}
