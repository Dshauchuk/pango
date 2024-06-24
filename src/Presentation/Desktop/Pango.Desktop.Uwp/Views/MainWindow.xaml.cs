using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;

namespace Pango.Desktop.Uwp.Views;

public sealed partial class MainWindow : Window
{
	public MainWindow ()
	{
		InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(this.TitleBarBorder);

        RootGrid.PointerMoved += RootGrid_PointerMoved;
        RootGrid.KeyDown += RootGrid_KeyDown;
    }

    public event EventHandler<PointerRoutedEventArgs> PointerMoved;
    public event EventHandler<KeyRoutedEventArgs> KeyDown;

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        PointerMoved?.Invoke(sender, e);
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        KeyDown?.Invoke(sender, e);
    }

}