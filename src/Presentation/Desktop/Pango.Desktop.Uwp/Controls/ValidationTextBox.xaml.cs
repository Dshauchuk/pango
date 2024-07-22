using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Pango.Desktop.Uwp.Controls;

// <summary>
/// A simple control that acts as a container for a documentation block.
/// </summary>
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_WarningIcon", Type = typeof(FontIcon))]
public sealed class ValidationTextBox : ContentControl
{
    /// <summary>
    /// The <see cref="TextBox"/> instance in use.
    /// </summary>
    private TextBox? _textBox;

    /// <summary>
    /// The <see cref="MarkdownTextBlock"/> instance in use.
    /// </summary>
    private FontIcon? _warningIcon;

    /// <summary>
    /// The previous data context in use.
    /// </summary>
    private INotifyDataErrorInfo? _oldDataContext;

    public ValidationTextBox()
    {
        DataContextChanged += ValidationTextBox_DataContextChanged;
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _textBox = (TextBox)GetTemplateChild("PART_TextBox");
        _warningIcon = (FontIcon)GetTemplateChild("PART_WarningIcon");

        _textBox.TextChanged += TextBox_TextChanged;

        this.GotFocus += ValidationTextBox_GotFocus;
    }

    /// <summary>
    /// Gets or sets the <see cref="string"/> representing the text to display.
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
    /// 
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly),
        typeof(bool),
        typeof(ValidationTextBox),
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
    /// The <see cref="DependencyProperty"/> backing <see cref="HeaderText"/>.
    /// </summary>
    public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
        nameof(HeaderText),
        typeof(string),
        typeof(ValidationTextBox),
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
    /// The <see cref="DependencyProperty"/> backing <see cref="PlaceholderText"/>.
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        nameof(PlaceholderText),
        typeof(string),
        typeof(ValidationTextBox),
        new PropertyMetadata(default(string)));

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
        typeof(ValidationTextBox),
        new PropertyMetadata(PropertyNameProperty, OnPropertyNamePropertyChanged));

    /// <summary>
    /// Invokes <see cref="RefreshErrors"/> whenever <see cref="PropertyName"/> changes.
    /// </summary>
    private static void OnPropertyNamePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is not string { Length: > 0 } propertyName)
        {
            return;
        }

        ((ValidationTextBox)sender).RefreshErrors();
    }

    /// <summary>
    /// Updates the bindings whenever the data context changes.
    /// </summary>
    private void ValidationTextBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
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

    private void ValidationTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _textBox?.Focus(FocusState.Programmatic);
    }

    /// <summary>
    /// Updates <see cref="Text"/> when needed.
    /// </summary>
    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Text = ((TextBox)sender).Text;
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
