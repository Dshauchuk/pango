using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.Controls;

// <summary>
/// A simple control that acts as a container for a documentation block.
/// </summary>
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_DeleteButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_FilterButton", Type = typeof(Button))]
public sealed class SearchTextBox : ContentControl
{
    /// <summary>
    /// The <see cref="TextBox"/> instance in use.
    /// </summary>
    private TextBox _textBox;
    private Button _deleteButton;
    private Button _searchButton;

    public SearchTextBox()
    {

    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _textBox = (TextBox)GetTemplateChild("PART_TextBox");
        _searchButton = (Button)GetTemplateChild("PART_FilterButton");
        _deleteButton = (Button)GetTemplateChild("PART_DeleteButton");

        _textBox.TextChanged += TextBox_TextChanged;
        _deleteButton.Click += _deleteButton_Click;
    }

    private void _deleteButton_Click(object sender, RoutedEventArgs e)
    {
        _textBox.Text = string.Empty; 
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
        typeof(SearchTextBox),
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
        typeof(SearchTextBox),
        new PropertyMetadata(default(string)));

    /// <summary>
    /// Updates <see cref="Text"/> when needed.
    /// </summary>
    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Text = ((TextBox)sender).Text;

        _deleteButton.Visibility = string.IsNullOrEmpty(Text) ? Visibility.Collapsed : Visibility.Visible;
    }
}
