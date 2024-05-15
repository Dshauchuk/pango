using System;

namespace Pango.Desktop.Uwp.Core.Utility.Contracts;

public interface IAppIdleService
{
    void StartAppIdle(TimeSpan timeOfIdle, Action onIdle);

    void StopAppIdle();
}