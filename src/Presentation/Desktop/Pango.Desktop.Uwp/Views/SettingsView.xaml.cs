﻿using CommunityToolkit.Mvvm.DependencyInjection;
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
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<SettingsViewModel>();
    }

    private async void bugRequestCard_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/Dshauchuk/pango/issues/new/choose"));
    }
}
