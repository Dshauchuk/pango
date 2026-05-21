using System.Reflection;

namespace Pango.Desktop.Uwp.Core.Utility;

/// <summary>
/// Formats the version shown in UI and export metadata. Strips SemVer 2.0 build metadata
/// (the part after '+', e.g. git commit id injected into AssemblyInformationalVersion).
/// </summary>
public static class AppVersionFormatter
{
    public static string GetDisplayVersion(Assembly? assembly)
    {
        if (assembly is null)
            return "undefined";

        var raw = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(raw))
            return StripSemVerBuildMetadata(raw);

        var version = assembly.GetName().Version;
        return version is null
            ? "undefined"
            : string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
    }

    public static string StripSemVerBuildMetadata(string informationalVersion)
    {
        var i = informationalVersion.IndexOf('+');
        var core = i < 0 ? informationalVersion : informationalVersion[..i];
        return core.Trim();
    }
}
