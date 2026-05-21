using System.Reflection;
using Pango.Application.Common.Interfaces.Services;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppMetaService : IAppMetaService
{
    public string GetAppVersion() => AppVersionFormatter.GetDisplayVersion(Assembly.GetEntryAssembly());
}
