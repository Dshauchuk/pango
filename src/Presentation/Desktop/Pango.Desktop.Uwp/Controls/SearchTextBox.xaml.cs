using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Pango.Desktop.Uwp.Controls;

// <summary>
/// A simple control that acts as a container for a documentation block.
/// </summary>
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_DeleteButton", Type = typeof(Button))]
[TemplatePart(Name = "PART_FilterButton", Type = typeof(Button))]
public sealed partial class SearchTextBox : ContentControl
{
    /// <summary>
    /// The <see cref="TextBox"/> instance in use.
    /// </summary>
    private TextBox? _textBox;
    private Button? _deleteButton;
    private Button? _searchButton;
    private readonly DispatcherTimer _debounceTimer;
    private bool _isInternalChange;

    public SearchTextBox()
    {
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _debounceTimer.Tick += DebounceTimer_Tick;
        Unloaded += (s, e) =>
        {
            _debounceTimer.Stop();
        };
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _textBox = (TextBox)GetTemplateChild("PART_TextBox");
        _searchButton = (Button)GetTemplateChild("PART_FilterButton");
        _deleteButton = (Button)GetTemplateChild("PART_DeleteButton");

        if (_textBox != null)
        {
            _textBox.LostFocus += (s, e) =>
            {
                _debounceTimer.Stop();
                if (!string.Equals(Text, _textBox.Text, StringComparison.Ordinal))
                {
                    Text = _textBox.Text;
                }
            };
            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.KeyUp += TextBox_KeyUp;
        }

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
        new PropertyMetadata(string.Empty, OnTextChanged));

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SearchTextBox control && control._textBox != null)
        {
            if (control._isInternalChange) return;

            string newValue = e.NewValue as string ?? string.Empty;
            if (control._textBox.Text != newValue)
            {
                control._isInternalChange = true;
                control._textBox.Text = newValue;
                control._isInternalChange = false;
            }
        }
    }

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
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            _debounceTimer.Stop();
            if (_textBox != null && !string.Equals(Text, _textBox.Text, StringComparison.Ordinal))
            {
                Text = _textBox.Text;
            }
            TriggerSearch();
        }
    }

    /// <summary>
    /// Updates <see cref="Text"/> when needed.
    /// </summary>
    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _deleteButton!.Visibility = string.IsNullOrEmpty(_textBox?.Text) ? Visibility.Collapsed : Visibility.Visible;

        if (!_isInternalChange && _textBox != null && Text != _textBox.Text)
        {
            _isInternalChange = true;
            Text = _textBox.Text;
            _isInternalChange = false;
        }

        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void DebounceTimer_Tick(object? sender, object e)
    {
        _debounceTimer.Stop();
        TriggerSearch();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        _textBox?.Text = string.Empty;
        _isInternalChange = true;
        Text = string.Empty;
        _isInternalChange = false;
        _debounceTimer.Stop();
        TriggerSearch();
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _debounceTimer.Stop();
        TriggerSearch();
    }

    private void TriggerSearch()
    {
        SearchCommand?.Execute(Text);
    }

    #endregion
}
