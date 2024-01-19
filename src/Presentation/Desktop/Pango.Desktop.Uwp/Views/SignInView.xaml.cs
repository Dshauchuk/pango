using CommunityToolkit.Mvvm.DependencyInjection;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

public sealed partial class SignInView : ViewBase
{
    public SignInView()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<SignInViewModel>();
    }

    private async void PasscodePasswordBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ((SignInViewModel)DataContext).SignInCommand.ExecuteAsync(null);
        }
    }
}
