using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.ViewModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// Represents the home view (dashboard) of the application.
/// </summary>
[AppView(AppView.Home)]
public sealed partial class HomeView : Page
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HomeView"/> class.
    /// </summary>
    public HomeView()
    {
        InitializeComponent();

        NavigationCacheMode = NavigationCacheMode.Enabled;

        DataContext = App.Host.Services.GetRequiredService<HomeViewModel>();
    }

    #region Hover Animations for Cards

    /// <summary>
    /// Animates the specific property of a UI element's transform.
    /// </summary>
    /// <param name="target">The UI element containing the CompositeTransform.</param>
    /// <param name="propertyPath">The name of the property to animate (e.g., "TranslateY", "Rotation").</param>
    /// <param name="toValue">The final value of the animation.</param>
    private static void AnimateTransform(UIElement target, string propertyPath, double toValue)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = toValue,
            Duration = new Duration(TimeSpan.FromMilliseconds(250)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(animation, target.RenderTransform);
        Storyboard.SetTargetProperty(animation, propertyPath);

        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    // Passwords Icon: Jumps up
    private void Passwords_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimateTransform(PasswordsIcon, "TranslateY", -10);
    private void Passwords_PointerExited(object sender, PointerRoutedEventArgs e) => AnimateTransform(PasswordsIcon, "TranslateY", 0);

    // Users Icon: Scales up
    private void Users_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        AnimateTransform(UsersIcon, "ScaleX", 1.15);
        AnimateTransform(UsersIcon, "ScaleY", 1.15);
    }
    private void Users_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        AnimateTransform(UsersIcon, "ScaleX", 1.0);
        AnimateTransform(UsersIcon, "ScaleY", 1.0);
    }

    // Export/Import Icon: Shifts slightly to the right
    private void Export_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimateTransform(ExportIcon, "TranslateX", 8);
    private void Export_PointerExited(object sender, PointerRoutedEventArgs e) => AnimateTransform(ExportIcon, "TranslateX", 0);

    // Generator Icon: Tilts to the left
    private void Generator_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimateTransform(GeneratorIcon, "Rotation", -15);
    private void Generator_PointerExited(object sender, PointerRoutedEventArgs e) => AnimateTransform(GeneratorIcon, "Rotation", 0);

    // Settings Icon: Spins around like a gear
    private void Settings_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimateTransform(SettingsIcon, "Rotation", 90);
    private void Settings_PointerExited(object sender, PointerRoutedEventArgs e) => AnimateTransform(SettingsIcon, "Rotation", 0);

    #endregion
}
