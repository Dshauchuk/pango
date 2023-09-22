using CommunityToolkit.Mvvm.DependencyInjection;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

public sealed partial class SignInView : ViewBase
{
    public SignInView()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<SignInViewModel>();
    }
}
