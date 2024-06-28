using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.Home)]
public sealed partial class HomeView : Page
{
    public HomeView()
    {
        this.InitializeComponent();
    }
}
