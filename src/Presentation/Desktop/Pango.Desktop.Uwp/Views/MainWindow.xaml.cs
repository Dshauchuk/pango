using Microsoft.UI.Xaml;

namespace Pango.Desktop.Uwp.Views;

public sealed partial class MainWindow : Window
{
	public MainWindow ()
	{
		InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(this.TitleBarBorder);
    }
}