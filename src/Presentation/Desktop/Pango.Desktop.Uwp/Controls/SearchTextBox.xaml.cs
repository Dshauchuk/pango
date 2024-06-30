using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
        _textBox.KeyUp += TextBox_KeyUp;
        _deleteButton.Click += DeleteButton_Click;
        _searchButton.Click += SearchButton_Click;
    }

    #region Properties

    /// <summary>
    /// Gets or sets the <see cref="ICommand"/> representing the command that's triggerred when the search button clicked
    /// </summary>
    public RelayCommand<string> SearchCommand
    {
        get => (RelayCommand<string>)GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    /// <summary>
    /// The <see cref="DependencyProperty"/> backing <see cref="Text"/>.
    /// </summary>
    public static readonly DependencyProperty SearchCommandProperty = DependencyProperty.Register(
        nameof(SearchCommand),
        typeof(ICommand),
        typeof(SearchTextBox),
        new PropertyMetadata(null));

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

    #endregion

    #region Handlers

    private void TextBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if(e.Key == Windows.System.VirtualKey.Enter)
        {
            TriggerSearch();
        }
    }

    /// <summary>
    /// Updates <see cref="Text"/> when needed.
    /// </summary>
    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Text = ((TextBox)sender).Text;

        _deleteButton.Visibility = string.IsNullOrEmpty(Text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        _textBox.Text = string.Empty;
        Text = string.Empty;

        TriggerSearch();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        TriggerSearch();
    }

    private void TriggerSearch()
    {
        SearchCommand?.Execute(Text);
    }

    #endregion
}
