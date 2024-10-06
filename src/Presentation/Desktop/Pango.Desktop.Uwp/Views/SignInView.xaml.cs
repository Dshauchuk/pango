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
        this.InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<SignInViewModel>();

        Loaded += SignInView_Loaded;

        if(App.Current.KeyboardHook != null)
        {
            App.Current.KeyboardHook.CapsLockChanged += KeyboardHook_CapsLockChanged;
        }
    }

    private void KeyboardHook_CapsLockChanged(object? sender, bool e)
    {
        HandleCapsLock();
    }

    private void HandleCapsLock()
    {
        Windows.UI.Core.CoreVirtualKeyStates capsLock =
            Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.CapitalLock);

        ((SignInViewModel)DataContext).IsCapLockWarningShown = capsLock.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Locked);
    }

    private void SignInView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PasscodePasswordBox.Focus(FocusState.Programmatic);
        HandleCapsLock();
    }

    private async void PasscodePasswordBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ((SignInViewModel)DataContext).SignInCommand.ExecuteAsync(null);
        }
    }
}
