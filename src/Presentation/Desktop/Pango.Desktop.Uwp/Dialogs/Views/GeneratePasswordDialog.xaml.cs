using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.ViewModels;
using Pango.Desktop.Uwp.ViewModels;
using System;

namespace Pango.Desktop.Uwp.Dialogs.Views;

public sealed partial class GeneratePasswordDialog : DialogPage
{
    public GeneratePasswordDialog(GeneratePasswordDialogParameters parameter) : base(parameter)
    {
        InitializeComponent();
        this.SetViewModel(App.Host.Services.GetRequiredService<GeneratePasswordDialogViewModel>());
    }
    public override string Title => ViewResourceLoader.GetString("GeneratePassword_Title");
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
    private void RegenerateButton_Click(object sender, RoutedEventArgs e)
    {
        AnimateRefreshIcon();
    }

    private void AnimateRefreshIcon()
    {
        var animation = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = new Duration(TimeSpan.FromMilliseconds(400)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, RefreshIcon);
        Storyboard.SetTargetProperty(animation, "(UIElement.RenderTransform).(CompositeTransform.Rotation)");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

}

