using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Controls;

public sealed partial class FiltrationTextBox : UserControl
{
    public FiltrationTextBox()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        "Text",
        typeof(string),
        typeof(FiltrationTextBox),
        new PropertyMetadata(null)
        );

    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(
        "PlaceholderText",
        typeof(string),
        typeof(FiltrationTextBox),
        new PropertyMetadata(null)
        );

    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }

    public string PlaceholderText
    {
        get { return (string)GetValue(PlaceholderTextProperty); }
        set { SetValue(PlaceholderTextProperty, value); }
    }
}
