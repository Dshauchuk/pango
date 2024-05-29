using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Core;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
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

    private Guid? _lockAppIdleId;

    public MainAppViewModel(IAppIdleService appIdleService, ILogger<MainAppViewModel> logger) : base(logger)
    {
        _appIdleService = appIdleService;
        WeakReferenceMessenger.Default.Register<AutolockIdleChangedMessage>(this, OnAutolockIdleChanged);
    }

    public override Task OnNavigatedToAsync(object parameter)
    {
        int? blockAppAfterIdleMinutes = (int?)ApplicationData.Current.LocalSettings.Values[Constants.Settings.BlockAppAfterIdleMinutes];

        if (blockAppAfterIdleMinutes.HasValue)
        {
            _lockAppIdleId = _appIdleService.StartAppIdle(TimeSpan.FromMinutes(blockAppAfterIdleMinutes.Value), OnLockIdleTimerElapsed);
        }
        return Task.CompletedTask;
    }

    public override Task OnNavigatedFromAsync(object parameter)
    {
        StopLockAppIdle();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles lock app idle time changing. Stops current lock app idle timer if <see cref="AutolockIdleChangedMessage"/>.Value is null, 
    /// otherwise - sets lock app idle timer to the passed value in minutes
    /// </summary>
    private void OnAutolockIdleChanged(object recipient, AutolockIdleChangedMessage message)
    {
        StopLockAppIdle();
        if (message.Value.HasValue)
        {
            _lockAppIdleId = _appIdleService.StartAppIdle(TimeSpan.FromMinutes(message.Value.Value), OnLockIdleTimerElapsed);
        }
    }

    /// <summary>
    /// Stops current lock app idle timer if it is active
    /// </summary>
    private void StopLockAppIdle()
    {
        if (_lockAppIdleId.HasValue)
        {
            _appIdleService.StopAppIdle(_lockAppIdleId.Value);
            _lockAppIdleId = null;
        }
    }

    /// <summary>
    /// Stops current lock app idle timer and navigates to the Sign In page
    /// </summary>
    private void OnLockIdleTimerElapsed()
    {
        StopLockAppIdle();
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new(new NavigationParameters(AppView.SignIn)));
    }
}
