using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Core;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Core.Utility.Contracts;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.MainAppView)]
public sealed class MainAppViewModel : ViewModelBase
{
    private readonly IAppIdleService _appIdleService;

    public MainAppViewModel(IAppIdleService appIdleService)
    {
        _appIdleService = appIdleService;
    }

    public override Task OnNavigatedToAsync(object parameter)
    {
        double? blockAppAfterIdleMinutes = (double?)ApplicationData.Current.LocalSettings.Values[Constants.Settings.BlockAppAfterIdleMinutes];

        if (blockAppAfterIdleMinutes.HasValue)
        {
            _appIdleService.StartAppIdle(TimeSpan.FromMinutes(blockAppAfterIdleMinutes.Value), OnIsIdleChanged);
        }
    }

    public override Task OnNavigatedFromAsync(object parameter)
    {
        AppIdleHelper.IsIdleChanged -= OnIsIdleChanged;
        return Task.CompletedTask;
    }

    private void OnIsIdleChanged(object sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new(new NavigationParameters(AppView.SignIn)));
    }
}
