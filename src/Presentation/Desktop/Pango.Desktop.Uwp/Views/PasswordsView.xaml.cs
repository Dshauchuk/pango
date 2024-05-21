using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.PasswordsIndex)]
public sealed partial class PasswordsView : PageBase
{
    public PasswordsView()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PasswordsViewModel>();

        PasswordsTreeView.ItemInvoked += PasswordsTreeView_ItemInvoked;
    }

    private void PasswordsTreeView_ItemInvoked(Windows.UI.Xaml.Controls.TreeView sender, Windows.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
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
}
