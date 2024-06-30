using Pango.Desktop.Uwp.Security;
using Pango.Persistence;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppUserProvider : IAppUserProvider
{
    /// <summary>
    /// Returns currently authenticated user ID
    /// </summary>
    /// <returns></returns>
    public string GetUserId()
    {
        return SecureUserSession.GetUser().UserName;
    }
}
