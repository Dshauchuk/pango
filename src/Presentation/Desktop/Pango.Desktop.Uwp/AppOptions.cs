using Pango.Application.Common.Interfaces;

namespace Pango.Desktop.Uwp;

public class AppOptions : IAppOptions
{
    public AppOptions(IFileOptions fileOptions) => FileOptions = fileOptions;

    public IFileOptions FileOptions { get; set; }
}

public class FileOptions : IFileOptions
{
    public int PasswordsPerFile { get; set; }
}
