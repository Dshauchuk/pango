using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.PasswordsIndex)]
public sealed partial class PasswordsView : PageBase
{
    public PasswordsView()
    {
        this.InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<PasswordsViewModel>();

        PasswordsTreeView.ItemInvoked += PasswordsTreeView_ItemInvoked;
    }

    private void PasswordsTreeView_ItemInvoked(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
    {
        PasswordsViewModel viewModel = DataContext as PasswordsViewModel;

        if (viewModel is not null)
        {
            viewModel.SelectedItem = args.InvokedItem as PasswordExplorerItem;
        }
    }

    protected override void RegisterMessages()
    {
        base.RegisterMessages();

        WeakReferenceMessenger.Default.Register<NavigationRequstedMessage>(this, OnNavigationRequested);
    }

    private void OnNavigationRequested(object recipient, NavigationRequstedMessage message)
    {
        switch(message.Value.NavigatedView)
        {
            case Core.Enums.AppView.EditPassword:
                PasswordsIndex_Pivot.SelectedIndex = 1;
                break;
            case Core.Enums.AppView.PasswordsIndex:
                PasswordsIndex_Pivot.SelectedIndex = 0;
                break;
        }
    }

    private void EditContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel viewModel = DataContext as PasswordsViewModel;
        viewModel?.EditPasswordCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem);
    }

    private void DeleteContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel viewModel = DataContext as PasswordsViewModel;
        viewModel?.DeleteCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem);
    }

    private void AddPassword_CatalogContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel viewModel = DataContext as PasswordsViewModel;
        viewModel.SelectedItem = ((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem;
        viewModel?.CreatePasswordCommand.Execute(null);
    }

    private void AddCatalog_CatalogContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel viewModel = DataContext as PasswordsViewModel;
        viewModel.SelectedItem = ((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem;
        viewModel?.CreateCatalogCommand.Execute(null);
    }

    private void CopyPassword_PasswordContextMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasswordsViewModel viewModel = DataContext as PasswordsViewModel;
        viewModel?.CopyPasswordToClipboardCommand.Execute(((MenuFlyoutItem)e.OriginalSource).DataContext as PasswordExplorerItem);
    }
}
