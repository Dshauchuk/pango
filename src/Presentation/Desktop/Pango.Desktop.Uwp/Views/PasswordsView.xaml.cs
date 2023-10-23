using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.PasswordsIndex)]
public sealed partial class PasswordsView : PageBase
{
    public PasswordsView()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<PasswordsViewModel>();
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
