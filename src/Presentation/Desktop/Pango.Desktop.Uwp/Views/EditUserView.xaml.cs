using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Input;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.EditUser)]
public sealed partial class EditUserView : ViewBase
{
    public EditUserView()
    {
        this.InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<EditUserViewModel>();

        this.KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Enter:
                ((EditUserViewModel)DataContext).SaveUserComand.Execute(null);
                break;
            default:
                break;
        }
    }
}
