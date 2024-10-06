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

// <summary>
/// A simple control that acts as a container for a documentation block.
/// </summary>
[TemplatePart(Name = "PART_PasswordBox", Type = typeof(PasswordBox))]
[TemplatePart(Name = "PART_WarningIcon", Type = typeof(FontIcon))]
public sealed class ValidationPasswordBox : ContentControl
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

    /// <summary>
    /// The previous data context in use.
    /// </summary>
    private INotifyDataErrorInfo? _oldDataContext;

    public ValidationPasswordBox()
    {
        DataContextChanged += ValidationPasswordBox_DataContextChanged;
    }

    /// <inheritdoc/>
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

        if(_copyButton is not null)
        {
            _copyButton.Click += CopyBtn_Click;
        }

        _passwordBox.PasswordChanged += PasswordBox_TextChanged;

        this.GotFocus += ValidationPasswordBox_GotFocus;

        TriggerActionButtonsVisibility();
    }

    /// <summary>
    /// Gets or sets the <see cref="string"/> representing the text to display.
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
        nameof(Password),
        typeof(string),
        typeof(ValidationPasswordBox),
        new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets the <see cref="string"/> representing the header text to display.
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
        nameof(HeaderText),
        typeof(string),
        typeof(ValidationPasswordBox),
        new PropertyMetadata(default(string)));

    /// <summary>
    /// 
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly),
        typeof(bool),
        typeof(ValidationPasswordBox),
        new PropertyMetadata(default(bool)));

    /// <summary>
    /// 
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// 
    /// </summary>
    public static readonly DependencyProperty HideCopyButtonProperty = DependencyProperty.Register(
        nameof(HideCopyButton),
        typeof(bool),
        typeof(ValidationPasswordBox),
        new PropertyMetadata(default(bool), OnHideCopyButtonChanged));

    /// <summary>
    /// 
    /// </summary>
    public bool HideCopyButton
    {
        get => (bool)GetValue(HideCopyButtonProperty);
        set => SetValue(HideCopyButtonProperty, value);
    }

    /// <summary>
    /// The <see cref="DependencyProperty"/> backing <see cref="PlaceholderText"/>.
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText),
        typeof(string),
        typeof(ValidationPasswordBox),
        new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets the <see cref="string"/> representing the placeholder text to display.
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="string"/> representing the text to display.
    /// </summary>
    public string PropertyName
    {
        get => (string)GetValue(PropertyNameProperty);
        set => SetValue(PropertyNameProperty, value);
    }

    /// <summary>
    /// The <see cref="DependencyProperty"/> backing <see cref="PropertyName"/>.
    /// </summary>
    public static readonly DependencyProperty PropertyNameProperty = DependencyProperty.Register(
        nameof(PropertyName),
        typeof(string),
        typeof(ValidationPasswordBox),
        new PropertyMetadata(PropertyNameProperty, OnPropertyNamePropertyChanged));

    private static void OnHideCopyButtonChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (((ValidationPasswordBox)sender!)._copyButton is not null)
        {
            ((ValidationPasswordBox)sender!)._copyButton!.Visibility = (bool)e.NewValue ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    /// Invokes <see cref="RefreshErrors"/> whenever <see cref="PropertyName"/> changes.
    /// </summary>
    private static void OnPropertyNamePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is not string { Length: > 0 } propertyName)
        {
            return;
        }

        ((ValidationPasswordBox)sender).RefreshErrors();
    }

    private void CopyBtn_Click(object sender, RoutedEventArgs e)
    {
        DataPackage dataPackage = new()
        {
            RequestedOperation = DataPackageOperation.Copy
        };
        dataPackage.SetText(Password ?? string.Empty);

        Clipboard.SetContent(dataPackage);

        WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(new ResourceLoader().GetString("PasswordCopiedToClipboard")));
    }

    private void RevealButton_Unchecked(object sender, RoutedEventArgs e)
    {
        if(_passwordBox != null)
        {
            _passwordBox.PasswordRevealMode = PasswordRevealMode.Hidden;
        }
    }

    private void RevealButton_Checked(object sender, RoutedEventArgs e)
    {
        if (_passwordBox != null)
        {
            _passwordBox.PasswordRevealMode = PasswordRevealMode.Visible;
        }
    }

    /// <summary>
    /// Updates the bindings whenever the data context changes.
    /// </summary>
    private void ValidationPasswordBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_oldDataContext is not null)
        {
            _oldDataContext.ErrorsChanged -= DataContext_ErrorsChanged;
        }

        if (args.NewValue is INotifyDataErrorInfo dataContext)
        {
            _oldDataContext = dataContext;
            _oldDataContext.ErrorsChanged += DataContext_ErrorsChanged;
        }

        RefreshErrors();
    }

    /// <summary>
    /// Invokes <see cref="RefreshErrors"/> whenever the data context requires it.
    /// </summary>
    private void DataContext_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        RefreshErrors();
    }

    private void TriggerActionButtonsVisibility()
    {
        if (_revealButton != null)
        {
            _revealButton.Visibility = string.IsNullOrWhiteSpace(Password) ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_copyButton != null)
        {
            _copyButton.Visibility = HideCopyButton || string.IsNullOrWhiteSpace(Password) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    /// Updates <see cref="Text"/> when needed.
    /// </summary>
    private void PasswordBox_TextChanged(object sender, RoutedEventArgs e)
    {
        Password = ((PasswordBox)sender).Password;
        TriggerActionButtonsVisibility();
    }

    private void ValidationPasswordBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _passwordBox?.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Refreshes the errors currently displayed.
    /// </summary>
    private void RefreshErrors()
    {
        if (this._warningIcon is not FontIcon warningIcon ||
            PropertyName is not string propertyName ||
            DataContext is not INotifyDataErrorInfo dataContext)
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
