using System;

namespace Pango.Desktop.Uwp.Core.Utility.Contracts;

public interface IAppIdleService
{
    /// <summary>
    /// Starts app idle timer. Passed action <paramref name="onIdle"/> will be invoked after <paramref name="timeOfIdle"/> of user inactivity within the app window
    /// </summary>
    /// <param name="timeOfIdle">Time interval of the user inactivity after which <paramref name="onIdle"/> handler will be invoked</param>
    /// <param name="onIdle">Action that will be performed after <paramref name="timeOfIdle"/> of the user inactivity</param>
    /// <returns>Id of app idle timer</returns>
    Guid StartAppIdle(TimeSpan timeOfIdle, Action onIdle);

    /// <summary>
    /// Stops app idle timer by id, frees all resources
    /// </summary>
    /// <param name="appIdleId">App idle operation id to stop</param>
    void StopAppIdle(Guid appIdleId);
}