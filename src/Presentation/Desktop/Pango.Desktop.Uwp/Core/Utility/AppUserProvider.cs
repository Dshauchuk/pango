using Pango.Persistence;
using System.Threading;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppUserProvider : IAppUserProvider
{
    public string GetUserId()
    {
        return Thread.CurrentPrincipal.Identity.Name;
    }
}
