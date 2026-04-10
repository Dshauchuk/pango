using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Pango.Desktop.Uwp.Controls;

/// <summary>
/// A simple control that acts as a container for a documentation block.
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
    /// The <see cref="MarkdownTextBlock"/> instance in use.
    /// </summary>
    private FontIcon? _warningIcon;
    private INotifyDataErrorInfo? _oldDataContext;
    private readonly DispatcherTimer _typingTimer;

    /// <summary>
    /// Initializes a new instance and sets up lifecycle event handlers to prevent memory leaks.
    /// </summary>
    public ValidationTextBox()
    {
        _typingTimer = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(300) };
        _typingTimer.Tick += TypingTimer_Tick;

        DataContextChanged += ValidationTextBox_DataContextChanged;
        Unloaded += ValidationTextBox_Unloaded;
    }

    /// <summary>
    /// Applies the control template and attaches internal event handlers.
    /// </summary>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _textBox = (TextBox)GetTemplateChild("PART_TextBox");
        _warningIcon = (FontIcon)GetTemplateChild("PART_WarningIcon");

        _textBox.TextChanged += TextBox_TextChanged;
        GotFocus += ValidationTextBox_GotFocus;
    }

    /// <summary>
    /// Prevents memory leaks by unsubscribing from all events.
    /// </summary>
    private void ValidationTextBox_Unloaded(object sender, RoutedEventArgs e)
    {
        _oldDataContext?.ErrorsChanged -= DataContext_ErrorsChanged;
        _oldDataContext = null;

        _textBox?.TextChanged -= TextBox_TextChanged;

        GotFocus -= ValidationTextBox_GotFocus;
        DataContextChanged -= ValidationTextBox_DataContextChanged;
        Unloaded -= ValidationTextBox_Unloaded;
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
        nameof(Text), typeof(string), typeof(ValidationTextBox), new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets the header text to display.
    /// </summary>
    public string HeaderText
    {
        get => (string)GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(ValidationTextBox), new PropertyMetadata(default(bool)));

    /// <summary>
    /// Gets or sets a value indicating whether the text box is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// The <see cref="DependencyProperty"/> backing <see cref="HeaderText"/>.
    /// </summary>
    public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
        nameof(HeaderText), typeof(string), typeof(ValidationTextBox), new PropertyMetadata(default(string)));

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
        nameof(PlaceholderText), typeof(string), typeof(ValidationTextBox), new PropertyMetadata(default(string)));

    /// <summary>
    /// Gets or sets the property name used for validation.
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
        nameof(PropertyName), typeof(string), typeof(ValidationTextBox), new PropertyMetadata(PropertyNameProperty, OnPropertyNamePropertyChanged));

    /// <summary>
    /// Invokes error refresh whenever the property name changes.
    /// </summary>
    private static void OnPropertyNamePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is string { Length: > 0 })
        {
            ((ValidationTextBox)sender).RefreshErrors();
        }
    }

    /// <summary>
    /// Updates bindings and securely manages event subscriptions when the data context changes.
    /// </summary>
    private void ValidationTextBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
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
    /// Focuses the internal text box programmatically.
    /// </summary>
    private void ValidationTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _textBox?.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Updates the bound text value when the internal text changes.
    /// </summary>
    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _typingTimer.Stop();
        _typingTimer.Start();
    }

    private void TypingTimer_Tick(object? sender, object e)
    {
        _typingTimer.Stop();
        if (_textBox != null && Text != _textBox.Text)
        {
            Text = _textBox.Text;
        }
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
