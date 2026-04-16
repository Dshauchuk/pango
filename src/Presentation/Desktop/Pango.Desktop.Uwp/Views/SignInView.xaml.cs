using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.SignIn)]
public sealed partial class SignInView : ViewBase
{
    public SignInView()
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<SignInViewModel>();
        Loaded += SignInView_Loaded;
    }

    private void HandleCapsLock()
    {
        Windows.UI.Core.CoreVirtualKeyStates capsLock =
            Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.CapitalLock);

        if (DataContext is SignInViewModel vm)
        {
            vm.IsCapLockWarningShown = capsLock.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Locked);
        }
    }

    private void SignInView_Loaded(object sender, RoutedEventArgs e)
    {
        PasscodePasswordBox.Focus(FocusState.Programmatic);
        HandleCapsLock();
    }

    private async void PasscodePasswordBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        HandleCapsLock();

        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ((SignInViewModel)DataContext).SignInCommand.ExecuteAsync(null);
        }
    }
}
