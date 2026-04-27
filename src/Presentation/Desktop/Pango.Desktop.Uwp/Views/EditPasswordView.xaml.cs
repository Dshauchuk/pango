using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.EditPassword)]
public sealed partial class EditPasswordView : ViewBase
{
    public EditPasswordView()
    {
        InitializeComponent();
        TitleTextBox.Focus(FocusState.Programmatic);
        DataContext = App.Host.Services.GetRequiredService<EditPasswordViewModel>();
        Loaded += EditPasswordView_Loaded;
        KeyDown += EditPasswordView_KeyDown;
    }

    private void EditPasswordView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (e.OriginalSource is TextBox textBox && textBox.AcceptsReturn)
            {
                return;
            }

            ((EditPasswordViewModel)DataContext).SavePasswordCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void EditPasswordView_Loaded(object sender, RoutedEventArgs e)
    {
        TitleTextBox.Focus(FocusState.Programmatic);
    }
}
