using Pango.Desktop.Uwp.Security;
using Pango.Persistence;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppUserProvider : IAppUserProvider
{
    public string GetUserId()
    {
        return SecureUserSession.GetUser().UserName;
    }
}
