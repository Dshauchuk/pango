using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;
using Pango.Domain.Enums;
using Pango.Infrastructure.Services;
using Pango.Persistence.File.IntegrationTests.Support;

namespace Pango.Persistence.File.IntegrationTests;

public class ExportImportIntegrationTests : IDisposable
{
    private readonly string _testRootPath;
    private readonly Mock<IAppDomainProvider> _domainProviderMock = new();
    private readonly Mock<IUserContextProvider> _userContextMock = new();
    private readonly ContentEncoder _realEncoder = new();
    private readonly PangoFileDataExporter _exporter;
    private readonly PangoFileDataImporter _importer;

    public ExportImportIntegrationTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), "PangoExportTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRootPath);

        _domainProviderMock.Setup(x => x.GetTempFolderPath()).Returns(_testRootPath);

        _userContextMock.Setup(x => x.GetUserName()).Returns("testuser");
        _userContextMock.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(TestEncoding.CreateRandomAes256());

        _exporter = new PangoFileDataExporter(_realEncoder, _domainProviderMock.Object, NullLogger<PangoFileDataExporter>.Instance);
        _importer = new PangoFileDataImporter(_realEncoder, _domainProviderMock.Object, _userContextMock.Object, NullLogger<PangoFileDataImporter>.Instance);
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
    public async Task Export_And_Import_RoundTrip_Succeeds()
    {
        try
        {
            var encoding = TestEncoding.CreateRandomAes256();
            var exportOptions = new ExportOptions("Export Description", encoding);
            var importOptions = new ImportOptions(encoding);

            var manifest = new PangoPackageManifest("testuser", DateTime.UtcNow.ToString("O"), "Desc", new Dictionary<ContentType, int> { { ContentType.Passwords, 1 } });

            var passwords = new List<PangoPassword>
            {
                new() { Id = Guid.NewGuid(), Name = "My Bank", Login = "user", Value = "123" }
            };

            var package = new ContentPackage("testuser", ContentType.Passwords, typeof(List<PangoPassword>).FullName!, 1, passwords, DateTimeOffset.UtcNow);

            string exportedFilePath = await _exporter.ExportAsync(manifest, [package], exportOptions);

            Assert.True(System.IO.File.Exists(exportedFilePath));
            Assert.Equal(".pngx", Path.GetExtension(exportedFilePath));

            var importResult = await _importer.ImportAsync(exportedFilePath, importOptions);

            Assert.NotNull(importResult);
            Assert.Equal("Desc", importResult.Manifest.Description);

            var dataPackages = importResult.ContentPackages.Where(p => p.Data != null).ToList();
            Assert.Single(dataPackages);

            var importedPasswords = dataPackages.First().Data as IEnumerable<PangoPassword>;
            Assert.NotNull(importedPasswords);
            Assert.Equal("My Bank", importedPasswords!.First().Name);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Test threw exception: {ex}");
        }
    }

    [Fact]
    public async Task Import_ThrowsPangoImportException_WhenFileDoesNotExist()
    {
        var importOptions = new ImportOptions(TestEncoding.CreateRandomAes256());
        string fakePath = Path.Combine(_testRootPath, "ghost.pngx");

        var ex = await Assert.ThrowsAsync<PangoImportException>(() => _importer.ImportAsync(fakePath, importOptions));
        Assert.Contains("doesn't exist", ex.Message);
    }

    [Fact]
    public async Task Import_ThrowsPangoImportException_WhenExtensionInvalid()
    {
        var importOptions = new ImportOptions(TestEncoding.CreateRandomAes256());
        string fakePath = Path.Combine(_testRootPath, "test.txt");
        System.IO.File.WriteAllText(fakePath, "dummy");

        var ex = await Assert.ThrowsAsync<PangoImportException>(() => _importer.ImportAsync(fakePath, importOptions));
        Assert.Contains("Invalid file extension", ex.Message);
    }

    [Fact]
    public async Task Import_ReadManifest_ReturnsDefault_OnCorruptedData()
    {
        // Arrange
        var options = new ImportOptions(TestEncoding.CreateRandomAes256());
        string path = Path.Combine(_testRootPath, "corrupt.pngx");

        await System.IO.File.WriteAllBytesAsync(path, [0x00, 0x01, 0x02, 0x03, 0x04]);

        // Act
        var result = await _importer.ReadManifestAsync(path, options);

        Assert.Equal(default, result);
    }
}
