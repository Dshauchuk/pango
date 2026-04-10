using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;
using Pango.Infrastructure.Services;
using Pango.Persistence.File.IntegrationTests.Support;

namespace Pango.Persistence.File.IntegrationTests;

public class PasswordFileRepositoryIntegrationTests
{
    private static (string Root, TestAppDomainProvider Provider, PasswordFileRepository Repo, EncodingOptions Encoding) CreateStore()
    {
        string root = Path.Combine(Path.GetTempPath(), "pango_pwd_int_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var provider = new TestAppDomainProvider { RootPath = root };
        var encoding = TestEncoding.CreateRandomAes256();
        var repo = new PasswordFileRepository(
            new ContentEncoder(),
            provider,
            NullLogger<PasswordFileRepository>.Instance,
            new TestAppOptions());
        return (root, provider, repo, encoding);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best effort cleanup for temp integration folder
        }
    }

    [Fact]
    public async Task Create_Find_Update_Delete_RoundTrip_Persists_Encrypted_OnDisk()
    {
        var (root, provider, repo, encoding) = CreateStore();
        string user = "user_" + Guid.NewGuid().ToString("N");
        var ctx = new FileRepositoryActionContext(encoding, user, provider.GetUserFolderPath(user));

        try
        {
            var id = Guid.NewGuid();
            var entry = new PangoPassword
            {
                Id = id,
                Name = "GitHub",
                Login = "me",
                CatalogPath = "",
            };
            entry.Value = "secret-value";

            await repo.CreateAsync(entry, ctx);

            string pwdDir = Path.Combine(provider.GetUserFolderPath(user), "passwords");
            Assert.True(Directory.Exists(pwdDir));
            Assert.NotEmpty(Directory.GetFiles(pwdDir, "*.pngdat"));

            var found = await repo.FindAsync(p => p.Id == id, ctx);
            Assert.NotNull(found);
            Assert.Equal("GitHub", found!.Name);
            Assert.Equal("secret-value", found.Value);

            entry.Name = "GitHub-2";
            entry.Value = "rotated";
            await repo.UpdateAsync(entry, ctx);

            var afterUpdate = await repo.FindAsync(p => p.Id == id, ctx);
            Assert.Equal("GitHub-2", afterUpdate!.Name);
            Assert.Equal("rotated", afterUpdate.Value);

            await repo.DeleteAsync(entry, ctx);
            var gone = await repo.FindAsync(p => p.Id == id, ctx);
            Assert.Null(gone);
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public async Task CreateBatch_QueryAsync_Returns_All_For_User()
    {
        var (root, provider, repo, encoding) = CreateStore();
        string user = "batch_" + Guid.NewGuid().ToString("N");
        var ctx = new FileRepositoryActionContext(encoding, user, provider.GetUserFolderPath(user));

        try
        {
            var a = new PangoPassword { Id = Guid.NewGuid(), Name = "A", CatalogPath = "" };
            var b = new PangoPassword { Id = Guid.NewGuid(), Name = "B", CatalogPath = "" };
            a.Value = "1";
            b.Value = "2";

            await repo.CreateAsync(new[] { a, b }, ctx);

            var all = (await repo.QueryAsync(_ => true, ctx)).ToList();
            Assert.Equal(2, all.Count);
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public async Task Invalid_Context_Throws_ArgumentException()
    {
        var (root, _, repo, _) = CreateStore();
        try
        {
            var bad = new Mock<IRepositoryActionContext>();

            var entry = new PangoPassword { Id = Guid.NewGuid(), Name = "X", CatalogPath = "" };
            entry.Value = "v";

            await Assert.ThrowsAsync<ArgumentException>(() => repo.CreateAsync(entry, bad.Object));
        }
        finally
        {
            TryDelete(root);
        }
    }
}
