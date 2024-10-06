using Pango.Application.Common.Interfaces.Services;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppMetaService : IAppMetaService
{
    public string GetAppVersion()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        return version is null ? "undefined" : string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
    }
}
