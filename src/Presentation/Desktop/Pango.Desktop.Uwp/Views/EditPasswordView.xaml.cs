using CommunityToolkit.Mvvm.DependencyInjection;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

public sealed partial class EditPasswordView : ViewBase
{
    public EditPasswordView()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<EditPasswordViewModel>();
    }
}
