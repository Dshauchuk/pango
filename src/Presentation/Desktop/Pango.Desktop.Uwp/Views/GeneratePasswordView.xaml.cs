using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
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
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        PlayEntranceAnimation();
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

    private void PlayEntranceAnimation()
    {
        // Fade + slide up the main content
        var contentPanel = this.FindName("RootGrid") as Grid;
        if (contentPanel == null) return;

        contentPanel.Opacity = 0;
        var transform = contentPanel.RenderTransform as CompositeTransform ?? new CompositeTransform();
        contentPanel.RenderTransform = transform;
        transform.TranslateY = 20;

        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var slideUp = new DoubleAnimation
        {
            From = 20,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(350)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var storyboard = new Storyboard();

        Storyboard.SetTarget(fadeIn, contentPanel);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");

        Storyboard.SetTarget(slideUp, contentPanel);
        Storyboard.SetTargetProperty(slideUp, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");

        storyboard.Children.Add(fadeIn);
        storyboard.Children.Add(slideUp);
        storyboard.Begin();
    }
}
