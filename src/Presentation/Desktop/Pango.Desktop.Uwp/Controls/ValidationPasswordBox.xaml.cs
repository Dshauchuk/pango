using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Pango.Desktop.Uwp.Mvvm.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.Controls;

/// <summary>
/// A simple control that acts as a container for a documentation block with password validation.
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
    /// The <see cref="FontIcon"/> instance used for validation warnings.
    /// </summary>
    private FontIcon? _warningIcon;
    private ToggleButton? _revealButton;
    private Button? _copyButton;
    private INotifyDataErrorInfo? _oldDataContext;
    private string? _lastErrorMessage;
    private bool _isSyncing;

    /// <summary>
    /// Initializes a new instance and sets up lifecycle event handlers to prevent memory leaks.
    /// </summary>
    public ValidationPasswordBox()
    {
        DataContextChanged += ValidationPasswordBox_DataContextChanged;

        Loaded += (s, e) =>
        {
            if (DataContext is INotifyDataErrorInfo dataContext && !ReferenceEquals(_oldDataContext, dataContext))
            {
                _oldDataContext?.ErrorsChanged -= DataContext_ErrorsChanged;
                _oldDataContext = dataContext;
                _oldDataContext.ErrorsChanged += DataContext_ErrorsChanged;
                RefreshErrors();
            }
        };

        Unloaded += (s, e) =>
        {
            _oldDataContext?.ErrorsChanged -= DataContext_ErrorsChanged;
            _oldDataContext = null;
        };
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

        if (_passwordBox != null)
        {
            if (!string.IsNullOrEmpty(Password))
            {
                _isSyncing = true;
                _passwordBox.Password = Password;
                _isSyncing = false;
            }
            _passwordBox.PasswordChanged += PasswordBox_TextChanged;
        }

        if (_revealButton is not null)
        {
            _revealButton.Checked += RevealButton_Checked;
            _revealButton.Unchecked += RevealButton_Unchecked;
        }

        _copyButton?.Click += CopyBtn_Click;

        GotFocus += ValidationPasswordBox_GotFocus;
        TriggerActionButtonsVisibility();
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
        nameof(Password),
        typeof(string),
        typeof(ValidationPasswordBox),
        new PropertyMetadata(string.Empty, OnPasswordChanged));

    /// <summary>
    /// Handles changes to the Password dependency property and syncs with internal PasswordBox.
    /// </summary>
    /// <param name="d">The ValidationPasswordBox instance.</param>
    /// <param name="e">Event arguments containing old and new values.</param>
    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ValidationPasswordBox control && control._passwordBox != null)
        {
            if (control._isSyncing) return;

            string newValue = e.NewValue as string ?? string.Empty;
            if (!string.Equals(control._passwordBox.Password, newValue, StringComparison.Ordinal))
            {
                control._isSyncing = true;
                control._passwordBox.Password = newValue;
                control.TriggerActionButtonsVisibility();
                control._isSyncing = false;
            }
        }
    }

    private void PasswordBox_TextChanged(object sender, RoutedEventArgs e)
    {
        TriggerActionButtonsVisibility();

        if (_isSyncing) return;

        if (_passwordBox != null && !string.Equals(Password, _passwordBox.Password, StringComparison.Ordinal))
        {
            _isSyncing = true;
            Password = _passwordBox.Password;
            _isSyncing = false;
        }
    }

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
        nameof(HeaderText),
        typeof(string), 
        typeof(ValidationPasswordBox),
        new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets a value indicating whether the password box is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// The <see cref="DependencyProperty"/> backing <see cref="IsReadOnly"/>.
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly),
        typeof(bool),
        typeof(ValidationPasswordBox), 
        new PropertyMetadata(default(bool)));

    /// <summary>
    /// Gets or sets a value indicating whether to hide the copy button.
    /// </summary>
    public bool HideCopyButton
    {
        get => (bool)GetValue(HideCopyButtonProperty);
        set => SetValue(HideCopyButtonProperty, value);
    }

    /// <summary>
    /// The <see cref="DependencyProperty"/> backing <see cref="HideCopyButton"/>.
    /// </summary>
    public static readonly DependencyProperty HideCopyButtonProperty = DependencyProperty.Register(
        nameof(HideCopyButton), 
        typeof(bool),
        typeof(ValidationPasswordBox),
        new PropertyMetadata(default(bool), OnHideCopyButtonChanged));

    /// <summary>
    /// Gets or sets the placeholder text to display.
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
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
    /// Gets or sets the property name used for validation binding.
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
        new PropertyMetadata(string.Empty, OnPropertyNamePropertyChanged));

    /// <summary>
    /// Updates the visibility of the copy button when the dependency property changes.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">Event arguments containing old and new values.</param>
    private static void OnHideCopyButtonChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (((ValidationPasswordBox)sender)._copyButton is Button copyBtn)
        {
            bool hide = (bool)e.NewValue;
            copyBtn.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    /// Invokes error refresh whenever the property name changes.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="args">Event arguments containing old and new values.</param>
    private static void OnPropertyNamePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is string { Length: > 0 })
            ((ValidationPasswordBox)sender).RefreshErrors();
    }

    /// <summary>
    /// Copies the current password to the clipboard and sends a notification.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">Routed event arguments.</param>
    private void CopyBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string textToCopy = _passwordBox?.Password ?? Password ?? string.Empty;
            if (string.IsNullOrEmpty(textToCopy)) return;

            var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetText(textToCopy);
            Clipboard.SetContent(dataPackage);
            Clipboard.Flush();

            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage(new ResourceLoader().GetString("PasswordCopiedToClipboard")));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard copy failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Hides the plain text password by setting reveal mode to Hidden.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">Routed event arguments.</param>
    private void RevealButton_Unchecked(object sender, RoutedEventArgs e) => _passwordBox!.PasswordRevealMode = PasswordRevealMode.Hidden;

    /// <summary>
    /// Reveals the plain text password by setting reveal mode to Visible.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">Routed event arguments.</param>
    private void RevealButton_Checked(object sender, RoutedEventArgs e) => _passwordBox!.PasswordRevealMode = PasswordRevealMode.Visible;

    /// <summary>
    /// Updates bindings and securely manages event subscriptions when the data context changes.
    /// </summary>
    /// <param name="sender">The framework element that raised the event.</param>
    /// <param name="args">Event arguments containing the new data context.</param>
    private void ValidationPasswordBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (ReferenceEquals(args.NewValue, _oldDataContext)) return;

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
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">Event arguments containing the property name with errors.</param>
    private void DataContext_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e) => RefreshErrors();

    /// <summary>
    /// Updates the visibility of the internal action buttons based on the password content.
    /// </summary>
    private void TriggerActionButtonsVisibility()
    {
        bool hasText = !string.IsNullOrEmpty(_passwordBox?.Password);

        if (_revealButton != null)
        {
            _revealButton.IsEnabled = hasText;
            _revealButton.Visibility = Visibility.Visible;
        }

        if (_copyButton != null)
        {
            _copyButton.IsEnabled = hasText;
            _copyButton.Visibility = HideCopyButton ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    /// Focuses the internal password box programmatically when the control receives focus.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">Routed event arguments.</param>
    private void ValidationPasswordBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, this))
            _passwordBox?.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Refreshes the visibility and tooltip of the validation warning icon based on current errors.
    /// </summary>
    private void RefreshErrors()
    {
        if (_warningIcon == null) return;

        if (string.IsNullOrEmpty(PropertyName) || DataContext is not INotifyDataErrorInfo dataContext)
        {
            if (_lastErrorMessage != null)
            {
                _warningIcon.Visibility = Visibility.Collapsed;
                ToolTipService.SetToolTip(_warningIcon, null);
                _lastErrorMessage = null;
            }
            return;
        }

        var result = dataContext.GetErrors(PropertyName).OfType<ValidationResult>().FirstOrDefault();
        string? newError = result?.ErrorMessage;

        if (_lastErrorMessage != newError)
        {
            _warningIcon.Visibility = result != null ? Visibility.Visible : Visibility.Collapsed;
            ToolTipService.SetToolTip(_warningIcon, newError);
            _lastErrorMessage = newError;
        }
    }
}
