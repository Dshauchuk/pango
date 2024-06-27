using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System;
using Windows.System;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.Settings)]
public sealed partial class SettingsView : PageBase
{
    public SettingsView()
        : base(App.Host.Services.GetRequiredService<ILogger<SettingsView>>())
    {
        this.InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<SettingsViewModel>();
    }

    private async void BugRequestCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/Dshauchuk/pango/issues/new/choose"));
    }
}
