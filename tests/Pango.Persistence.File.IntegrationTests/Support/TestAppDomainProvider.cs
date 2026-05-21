using Pango.Persistence;

namespace Pango.Persistence.File.IntegrationTests.Support;

/// <summary>Maps each user to an isolated folder under a temporary root (integration tests only).</summary>
public sealed class TestAppDomainProvider : IAppDomainProvider
{
    public void ResetCache() { }

    public required string RootPath { get; init; }

    public string GetAppDataFolderPath() => RootPath;

    public string GetUserFolderPath(string userName) => Path.Combine(RootPath, userName);

    public string GetPath(string userName, params string[] pathElements) =>
        Path.Combine([GetUserFolderPath(userName), .. pathElements]);

    public string GetTempFolderPath() => Path.Combine(RootPath, "_temp");

    public Task<string?> TryGetCustomDataFolderPathAsync() => Task.FromResult<string?>(null);
}
