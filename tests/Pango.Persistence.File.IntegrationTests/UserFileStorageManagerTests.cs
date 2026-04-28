using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;

namespace Pango.Persistence.File.IntegrationTests;

public class UserFileStorageManagerTests : IDisposable
{
    private readonly string _testRootPath;
    private readonly Mock<IPasswordRepository> _passwordRepoMock = new();
    private readonly Mock<IAppDomainProvider> _domainProviderMock = new();
    private readonly Mock<IUserContextProvider> _userContextMock = new();
    private readonly Mock<IRepositoryContextFactory> _contextFactoryMock = new();
    private readonly UserFileStorageManager _manager;

    public UserFileStorageManagerTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), "PangoTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRootPath);

        _domainProviderMock.Setup(x => x.GetUserFolderPath(It.IsAny<string>()))
                    .Returns<string>(user => Path.Combine(_testRootPath, user));

        _manager = new UserFileStorageManager(
            _passwordRepoMock.Object,
            _domainProviderMock.Object,
            _userContextMock.Object,
            _contextFactoryMock.Object,
            NullLogger<UserFileStorageManager>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRootPath))
        {
            try { Directory.Delete(_testRootPath, true); } catch { }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task DeleteAllUserDataAsync_DeletesFolder()
    {
        string userFolder = Path.Combine(_testRootPath, "user1");
        Directory.CreateDirectory(userFolder);
        System.IO.File.WriteAllText(Path.Combine(userFolder, "test.txt"), "data");

        await _manager.DeleteAllUserDataAsync("user1");

        await Task.Delay(100);

        Assert.False(Directory.Exists(userFolder));
    }

    [Fact]
    public async Task MigrateDataAsync_CopiesAndDeletesOldFolder()
    {
        string oldRoot = Path.Combine(_testRootPath, "old");
        string newRoot = Path.Combine(_testRootPath, "new");
        string oldUsersDir = Path.Combine(oldRoot, AppConstants.UsersFolderName);
        string newUsersDir = Path.Combine(newRoot, AppConstants.UsersFolderName);

        Directory.CreateDirectory(oldUsersDir);
        System.IO.File.WriteAllText(Path.Combine(oldUsersDir, "file.txt"), "migrate me");

        await _manager.MigrateDataAsync(oldRoot, newRoot);

        await Task.Delay(100);

        Assert.False(Directory.Exists(oldUsersDir));
        Assert.True(Directory.Exists(newUsersDir));
        Assert.True(System.IO.File.Exists(Path.Combine(newUsersDir, "file.txt")));
    }

    [Fact]
    public async Task EncryptDataWithAsync_RoundTripsData_ThroughTempFolders()
    {
        string userId = "enc_user";
        var userFolder = Path.Combine(_testRootPath, userId);
        Directory.CreateDirectory(userFolder);

        _userContextMock.Setup(x => x.GetUserName()).Returns(userId);
        _userContextMock.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k1", "s1"));
        _contextFactoryMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>()))
            .Returns<string, EncodingOptions>((user, enc) =>
                new FileRepositoryActionContext(enc, user, Path.Combine(_testRootPath, user)));

        var fakePasswords = new[] { new Domain.Entities.PangoPassword { Name = "Test" } };
        _passwordRepoMock.Setup(x => x.QueryAsync(It.IsAny<Func<Domain.Entities.PangoPassword, bool>>(), It.IsAny<IRepositoryActionContext>()))
            .ReturnsAsync(fakePasswords);

        _passwordRepoMock.Setup(x => x.CreateAsync(It.IsAny<IEnumerable<Domain.Entities.PangoPassword>>(), It.IsAny<IRepositoryActionContext>()))
            .Callback<IEnumerable<Domain.Entities.PangoPassword>, IRepositoryActionContext>((items, ctx) =>
            {
                var fileCtx = (FileRepositoryActionContext)ctx;
                Directory.CreateDirectory(fileCtx.WorkingDirectoryPath);
            })
            .Returns(Task.CompletedTask);

        var newEncoding = new EncodingOptions("k2", "s2");

        // Act
        await _manager.EncryptDataWithAsync(userId, newEncoding);
        await Task.Delay(200);

        // Assert
        _passwordRepoMock.Verify(x => x.QueryAsync(It.IsAny<Func<Domain.Entities.PangoPassword, bool>>(), It.IsAny<IRepositoryActionContext>()), Times.AtLeastOnce);
        _passwordRepoMock.Verify(x => x.CreateAsync(fakePasswords, It.IsAny<IRepositoryActionContext>()), Times.Once);

        Assert.True(Directory.Exists(userFolder));
    }
}
