using Pango.Application.Common.Interfaces;

namespace Pango.Persistence.File.IntegrationTests.Support;

public sealed class TestFileOptions : IFileOptions
{
    public int PasswordsPerFile { get; set; } = 20;
}

public sealed class TestAppOptions : IAppOptions
{
    public IFileOptions FileOptions { get; set; } = new TestFileOptions();
}
