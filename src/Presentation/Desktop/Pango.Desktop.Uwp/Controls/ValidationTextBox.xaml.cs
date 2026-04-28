using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pango.Desktop.Uwp.Controls;

/// <summary>
/// A simple control that acts as a container for a validation text block.
/// </summary>
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_WarningIcon", Type = typeof(FontIcon))]
public sealed partial class ValidationTextBox : ContentControl
{
    /// <summary>
    /// The <see cref="TextBox"/> instance in use.
    /// </summary>
    private TextBox? _textBox;

    /// <summary>
    /// The <see cref="FontIcon"/> instance used for validation warnings.
    /// </summary>
    private FontIcon? _warningIcon;
    private INotifyDataErrorInfo? _oldDataContext;
    private string? _lastErrorMessage;
    private bool _isSyncing;

    /// <summary>
    /// Initializes a new instance and sets up lifecycle event handlers to prevent memory leaks.
    /// </summary>
    public ValidationTextBox()
    {
        Log.Logger?.Debug("ValidationTextBox initialized");
        DataContextChanged += ValidationTextBox_DataContextChanged;

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

        _textBox = (TextBox)GetTemplateChild("PART_TextBox");
        _warningIcon = (FontIcon)GetTemplateChild("PART_WarningIcon");

        if (_textBox != null)
        {
            _textBox.TextChanged += TextBox_TextChanged;
            if (!string.IsNullOrEmpty(Text))
            {
                _isSyncing = true;
                _textBox.Text = Text;
                _isSyncing = false;
            }
        }

        GotFocus += ValidationTextBox_GotFocus;
    }

    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// The <see cref="DependencyProperty"/> backing <see cref="Text"/>.
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(ValidationTextBox),
        new PropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>
    /// Handles changes to the Text dependency property and syncs with internal TextBox.
    /// </summary>
    /// <param name="d">The ValidationTextBox instance.</param>
    /// <param name="e">Event arguments containing old and new values.</param>
    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ValidationTextBox control && control._textBox != null)
        {
            if (control._isSyncing) return;

            string newValue = e.NewValue as string ?? string.Empty;
            if (!string.Equals(control._textBox.Text, newValue, StringComparison.Ordinal))
            {
                control._isSyncing = true;

                int start = control._textBox.SelectionStart;
                int length = control._textBox.SelectionLength;

                control._textBox.Text = newValue;

                if (start <= newValue.Length)
                {
                    control._textBox.SelectionStart = start;
                    control._textBox.SelectionLength = length;
                }
                else
                {
                    control._textBox.SelectionStart = newValue.Length;
                }

                control._isSyncing = false;
            }
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSyncing) return;

        if (_textBox != null && !string.Equals(Text, _textBox.Text, StringComparison.Ordinal))
        {
            _isSyncing = true;
            Text = _textBox.Text;
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
        typeof(ValidationTextBox),
        new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets a value indicating whether the text box is read-only.
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
        typeof(ValidationTextBox),
        new PropertyMetadata(default(bool)));

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
        typeof(ValidationTextBox),
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
        typeof(ValidationTextBox),
        new PropertyMetadata(string.Empty, OnPropertyNamePropertyChanged));

    /// <summary>
    /// Invokes error refresh whenever the property name changes.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="args">Event arguments containing old and new values.</param>
    private static void OnPropertyNamePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is string { Length: > 0 })
            ((ValidationTextBox)sender).RefreshErrors();
    }

    /// <summary>
    /// Updates bindings and securely manages event subscriptions when the data context changes.
    /// </summary>
    /// <param name="sender">The framework element that raised the event.</param>
    /// <param name="args">Event arguments containing the new data context.</param>
    private void ValidationTextBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
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
    private void DataContext_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        RefreshErrors();
    }

    /// <summary>
    /// Focuses the internal text box programmatically when the control receives focus.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">Routed event arguments.</param>
    private void ValidationTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, this))
            _textBox?.Focus(FocusState.Programmatic);
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
