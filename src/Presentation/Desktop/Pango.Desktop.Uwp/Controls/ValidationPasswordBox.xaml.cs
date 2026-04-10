using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Pango.Desktop.Uwp.Mvvm.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.Controls;

/// <summary>
/// A simple control that acts as a container for a documentation block.
/// </summary>
[TemplatePart(Name = "PART_PasswordBox", Type = typeof(PasswordBox))]
[TemplatePart(Name = "PART_WarningIcon", Type = typeof(FontIcon))]
public sealed partial class ValidationPasswordBox : ContentControl
{
    /// <summary>
    /// The <see cref="PasswordBox"/> instance in use.
    /// </summary>
    private PasswordBox? _passwordBox;

    /// <summary>
    /// The <see cref="MarkdownTextBlock"/> instance in use.
    /// </summary>
    private FontIcon? _warningIcon;
    private ToggleButton? _revealButton;
    private Button? _copyButton;
    private INotifyDataErrorInfo? _oldDataContext;
    private readonly DispatcherTimer _typingTimer;

    /// <summary>
    /// Initializes a new instance and sets up lifecycle event handlers to prevent memory leaks.
    /// </summary>
    public ValidationPasswordBox()
    {
        _typingTimer = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(300) };
        _typingTimer.Tick += TypingTimer_Tick;

        DataContextChanged += ValidationPasswordBox_DataContextChanged;
        Unloaded += ValidationPasswordBox_Unloaded;
    }

    /// <summary>
    /// Applies the control template and attaches internal event handlers.
    /// </summary>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _passwordBox = (PasswordBox)GetTemplateChild("PART_PasswordBox");
        _warningIcon = (FontIcon)GetTemplateChild("PART_WarningIcon");
        _copyButton = GetTemplateChild("CopyButton") as Button;
        _revealButton = GetTemplateChild("RevealButton") as ToggleButton;

        if (_revealButton is not null)
        {
            _revealButton.Checked += RevealButton_Checked;
            _revealButton.Unchecked += RevealButton_Unchecked;
        }

        _copyButton?.Click += CopyBtn_Click;

        _passwordBox.PasswordChanged += PasswordBox_TextChanged;
        GotFocus += ValidationPasswordBox_GotFocus;

        TriggerActionButtonsVisibility();
    }

    /// <summary>
    /// Prevents memory leaks by unsubscribing from all events when the control is removed from the visual tree.
    /// </summary>
    private void ValidationPasswordBox_Unloaded(object sender, RoutedEventArgs e)
    {
        _oldDataContext?.ErrorsChanged -= DataContext_ErrorsChanged;
        _oldDataContext = null;

        if (_revealButton is not null)
        {
            _revealButton.Checked -= RevealButton_Checked;
            _revealButton.Unchecked -= RevealButton_Unchecked;
        }

        _copyButton?.Click -= CopyBtn_Click;

        _passwordBox?.PasswordChanged -= PasswordBox_TextChanged;

        GotFocus -= ValidationPasswordBox_GotFocus;
        DataContextChanged -= ValidationPasswordBox_DataContextChanged;
        Unloaded -= ValidationPasswordBox_Unloaded;
    }

    /// <summary>
    /// Gets or sets the password value.
    /// </summary>
    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    /// <summary>
    /// The <see cref="DependencyProperty"/> backing <see cref="Password"/>.
    /// </summary>
    public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
        nameof(Password), typeof(string), typeof(ValidationPasswordBox), new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets the header text to display.
    /// </summary>
    public string HeaderText
    {
        get => (string)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    /// <summary>
    /// The <see cref="DependencyProperty"/> backing <see cref="HeaderText"/>.
    /// </summary>
    public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
        nameof(HeaderText), typeof(string), typeof(ValidationPasswordBox), new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets a value indicating whether the password box is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(ValidationPasswordBox), new PropertyMetadata(default(bool)));

    /// <summary>
    /// Gets or sets a value indicating whether to hide the copy button.
    /// </summary>
    public bool HideCopyButton
    {
        get => (bool)GetValue(HideCopyButtonProperty);
        set => SetValue(HideCopyButtonProperty, value);
    }

    public static readonly DependencyProperty HideCopyButtonProperty = DependencyProperty.Register(
        nameof(HideCopyButton), typeof(bool), typeof(ValidationPasswordBox), new PropertyMetadata(default(bool), OnHideCopyButtonChanged));

    /// <summary>
    /// Gets or sets the placeholder text to display.
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText), typeof(string), typeof(ValidationPasswordBox), new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets the property name used for validation.
    /// </summary>
    public string PropertyName
    {
        get => (string)GetValue(PropertyNameProperty);
        set => SetValue(PropertyNameProperty, value);
    }

    public static readonly DependencyProperty PropertyNameProperty = DependencyProperty.Register(
        nameof(PropertyName), typeof(string), typeof(ValidationPasswordBox), new PropertyMetadata(PropertyNameProperty, OnPropertyNamePropertyChanged));

    /// <summary>
    /// Updates the visibility of the copy button when the dependency property changes.
    /// </summary>
    private static void OnHideCopyButtonChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (((ValidationPasswordBox)sender)._copyButton is not null)
        {
            ((ValidationPasswordBox)sender)._copyButton!.Visibility = (bool)e.NewValue ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    /// Invokes error refresh whenever the property name changes.
    /// </summary>
    private static void OnPropertyNamePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is string { Length: > 0 })
        {
            ((ValidationPasswordBox)sender).RefreshErrors();
        }
    }

    /// <summary>
    /// Copies the current password to the clipboard and sends a notification.
    /// </summary>
    private void CopyBtn_Click(object sender, RoutedEventArgs e)
    {
        DataPackage dataPackage = new() { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText(Password ?? string.Empty);
        Clipboard.SetContent(dataPackage);
        WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(new ResourceLoader().GetString("PasswordCopiedToClipboard")));
    }

    /// <summary>
    /// Hides the plain text password.
    /// </summary>
    private void RevealButton_Unchecked(object sender, RoutedEventArgs e)
    {
        _passwordBox?.PasswordRevealMode = PasswordRevealMode.Hidden;
    }

    /// <summary>
    /// Reveals the plain text password.
    /// </summary>
    private void RevealButton_Checked(object sender, RoutedEventArgs e)
    {
        _passwordBox?.PasswordRevealMode = PasswordRevealMode.Visible;
    }

    /// <summary>
    /// Updates bindings and securely manages event subscriptions when the data context changes.
    /// </summary>
    private void ValidationPasswordBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        _oldDataContext?.ErrorsChanged -= DataContext_ErrorsChanged;

        if (args.NewValue is INotifyDataErrorInfo dataContext)
        {
            _oldDataContext = dataContext;
            _oldDataContext.ErrorsChanged += DataContext_ErrorsChanged;
        }

        RefreshErrors();
    }

    /// <summary>
    /// Invokes error refresh when the data context reports validation changes.
    /// </summary>
    private void DataContext_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        RefreshErrors();
    }

    /// <summary>
    /// Updates the visibility of the internal action buttons based on the password content.
    /// </summary>
    private void TriggerActionButtonsVisibility()
    {
        _revealButton?.Visibility = string.IsNullOrWhiteSpace(Password) ? Visibility.Collapsed : Visibility.Visible;

        _copyButton?.Visibility = HideCopyButton || string.IsNullOrWhiteSpace(Password) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Updates the bound password value when the internal text changes.
    /// </summary>
    private void PasswordBox_TextChanged(object sender, RoutedEventArgs e)
    {
        TriggerActionButtonsVisibility();
        _typingTimer.Stop();
        _typingTimer.Start();
    }

    private void TypingTimer_Tick(object? sender, object e)
    {
        _typingTimer.Stop();
        if (_passwordBox != null && Password != _passwordBox.Password)
        {
            Password = _passwordBox.Password;
        }
    }

    /// <summary>
    /// Focuses the internal password box programmatically.
    /// </summary>
    private void ValidationPasswordBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _passwordBox?.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Refreshes the visibility and tooltip of the validation warning icon.
    /// </summary>
    private void RefreshErrors()
    {
        if (_warningIcon is not FontIcon warningIcon || PropertyName is not string propertyName || DataContext is not INotifyDataErrorInfo dataContext)
        {
            return;
        }

        ValidationResult? result = dataContext.GetErrors(propertyName).OfType<ValidationResult>().FirstOrDefault();
        warningIcon.Visibility = result is not null ? Visibility.Visible : Visibility.Collapsed;

        if (result is not null)
        {
            ToolTipService.SetToolTip(warningIcon, result.ErrorMessage);
        }
    }
}
