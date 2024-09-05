using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.ViewModels;
using System;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pango.Desktop.Uwp.Dialogs.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ExportCompletedDialog : DialogPage
{
    public ExportCompletedDialog(ExportResultParameters parameter) : base(parameter)
    {
        this.InitializeComponent();
        this.SetViewModel(App.Host.Services.GetRequiredService<ExportCompletedDialogViewModel>());
    }

    public override string Title => ViewResourceLoader.GetString("ExportResultTitle");

    private void HyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string link = ((HyperlinkButton)sender).Content as string;

        try
        {
            ProcessStartInfo processStart = new ProcessStartInfo(link)
            {
                UseShellExecute = true,
                Verb = "explore"
            };

            Process.Start(processStart);
        }
        catch (Exception ex)
        {

        }
    }
}
