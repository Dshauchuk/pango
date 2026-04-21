using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// View for the sign-in screen, handling user authentication UI interactions.
/// </summary>
[AppView(AppView.SignIn)]
public sealed partial class SignInView : ViewBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignInView"/> class.
    /// </summary>
    public SignInView()
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<SignInViewModel>();
        Loaded += SignInView_Loaded;
    }

    /// <summary>
    /// Checks the Caps Lock state and updates the view model warning flag accordingly.
    /// </summary>
    private void HandleCapsLock()
    {
        Windows.UI.Core.CoreVirtualKeyStates capsLock =
            Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.CapitalLock);

        if (DataContext is SignInViewModel vm)
        {
            vm.IsCapLockWarningShown = capsLock.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Locked);
        }
    }

    /// <summary>
    /// Handles the Loaded event: focuses the passcode box and checks Caps Lock state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data.</param>
    private void SignInView_Loaded(object sender, RoutedEventArgs e)
    {
        PasscodePasswordBox.Focus(FocusState.Programmatic);
        HandleCapsLock();
    }

    /// <summary>
    /// Handles key down events on the passcode box asynchronously; submits on Enter key.
    /// </summary>
    /// <param name="_">The event source (unused).</param>
    /// <param name="e">Key event arguments containing the pressed key.</param>
    private async void PasscodePasswordBox_KeyDownAsync(object _, KeyRoutedEventArgs e)
    {
        HandleCapsLock();

        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var vm = (SignInViewModel)DataContext;
            vm.Passcode = PasscodePasswordBox.Password;
            await vm.SignInCommand.ExecuteAsync(null);
        }
    }
}
