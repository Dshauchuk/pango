using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[AppView(Core.Enums.AppView.GeneratePassword)]
public sealed partial class GeneratePasswordView : PageBase
{
    public GeneratePasswordView()
        : base(App.Host.Services.GetRequiredService<ILogger<GeneratePasswordView>>())
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<GeneratePasswordViewModel>();

        LengthTextBox.KeyDown += LengthTextBox_KeyDown;
    }

    private void LengthTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;

            var binding = LengthTextBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();

            // launch generation
            if (DataContext is GeneratePasswordViewModel vm && vm.GenerateCommand.CanExecute(null))
            {
                vm.GenerateCommand.Execute(null);
            }
        }
    }
}
