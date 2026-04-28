using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Application.UseCases.Data.Commands.Import;
using Pango.Domain.Entities;
using Pango.Domain.Enums;

namespace Pango.Application.Tests.Data;

public class ImportDataCommandHandlerTests
{
    private readonly Mock<IDataImporter> _importer = new();
    private readonly Mock<IPasswordRepository> _passwords = new();
    private readonly Mock<IUserContextProvider> _ctx = new();
    private readonly Mock<IRepositoryContextFactory> _factory = new();
    private readonly Mock<IRepositoryActionContext> _action = new();
    private readonly Mock<ILogger<ImportDataCommandHandler>> _logger = new();

    public ImportDataCommandHandlerTests()
    {
        _ctx.Setup(x => x.GetUserName()).Returns("alice");
        _ctx.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k", "s"));
        _factory.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>())).Returns(_action.Object);
    }

    [Fact]
    public async Task Handle_CreatesImportedRoot_WhenSeparateFolder_AndEmptyPackages()
    {
        var manifest = new PangoPackageManifest("bob", DateTime.UtcNow.ToString("G"), "d", []);
        var packages = new List<IContentPackage>
        {
            new ContentPackage("bob", ContentType.Passwords, typeof(List<PangoPassword>).FullName!, 0, new List<PangoPassword>(), DateTimeOffset.UtcNow),
        };
        var dto = new ImportResultDto(manifest, packages);

        _importer.Setup(x => x.ImportAsync("c:\\a.pngx", It.IsAny<IImportOptions>())).ReturnsAsync(dto);

        _passwords.Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync([]);
        _passwords.Setup(x => x.CreateAsync(It.IsAny<PangoPassword>(), _action.Object)).Returns(Task.CompletedTask);

        var opts = new Mock<IImportOptions>();
        opts.Setup(o => o.EncodingOptions).Returns(new EncodingOptions("k", "s"));

        var handler = new ImportDataCommandHandler(_importer.Object, _passwords.Object, _factory.Object, _ctx.Object, _logger.Object);
        var result = await handler.Handle(new ImportDataCommand("c:\\a.pngx", opts.Object, null, importToSeparateFolder: true), CancellationToken.None);

        Assert.False(result.IsError);
        _passwords.Verify(x => x.CreateAsync(It.Is<PangoPassword>(p => p.IsCatalog && (p.Name == "Imported" || p.Name == "Імпартаванае")), _action.Object), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenImporterThrows()
    {
        _importer.Setup(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<IImportOptions>())).ThrowsAsync(new IOException("bad file"));

        var opts = new Mock<IImportOptions>();
        var handler = new ImportDataCommandHandler(_importer.Object, _passwords.Object, _factory.Object, _ctx.Object, _logger.Object);

        var result = await handler.Handle(new ImportDataCommand("x", opts.Object), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Data.ImportError, result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_ImportsToRoot_And_ReconstructsHierarchy_WhenNotSeparateFolder()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var importedItems = new List<PangoPassword>
        {
            new() { Id = parentId, Name = "ImportedFolder", IsCatalog = true, CatalogPath = "" },
            new() { Id = childId, Name = "ImportedPass", IsCatalog = false, CatalogPath = "ImportedFolder" }
        };

        var manifest = new PangoPackageManifest("user", DateTime.UtcNow.ToString("G"), "desc", []);
        var packages = new List<IContentPackage>
        {
            new ContentPackage("user", ContentType.Passwords, typeof(List<PangoPassword>).FullName!, 2, importedItems, DateTimeOffset.UtcNow)
        };
        var dto = new ImportResultDto(manifest, packages);

        _importer.Setup(x => x.ImportAsync("c:\\file.pngx", It.IsAny<IImportOptions>())).ReturnsAsync(dto);

        var opts = new Mock<IImportOptions>();
        opts.Setup(o => o.EncodingOptions).Returns(new EncodingOptions("key", "salt"));

        var handler = new ImportDataCommandHandler(_importer.Object, _passwords.Object, _factory.Object, _ctx.Object, _logger.Object);

        // Act
        var result = await handler.Handle(new ImportDataCommand("c:\\file.pngx", opts.Object, null, importToSeparateFolder: false), CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _passwords.Verify(x => x.CreateAsync(It.Is<IEnumerable<PangoPassword>>(list => list.Count() == 2), _action.Object), Times.Once);
    }

    [Fact]
    public async Task Handle_FiltersBySelectedIds_And_IncludesParentsAutomatically()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var unselectedId = Guid.NewGuid();

        var importedItems = new List<PangoPassword>
        {
            new() { Id = parentId, Name = "ParentFolder", IsCatalog = true, CatalogPath = "" },
            new() { Id = childId, Name = "SelectedChild", IsCatalog = false, CatalogPath = "ParentFolder" },
            new() { Id = unselectedId, Name = "IgnoredChild", IsCatalog = false, CatalogPath = "ParentFolder" }
        };

        var manifest = new PangoPackageManifest("user", "date", "desc", []);
        var packages = new List<IContentPackage>
        {
            new ContentPackage("user", ContentType.Passwords, "dataType", 3, importedItems, DateTimeOffset.UtcNow)
        };
        var dto = new ImportResultDto(manifest, packages);

        _importer.Setup(x => x.ImportAsync(It.IsAny<string>(), It.IsAny<IImportOptions>())).ReturnsAsync(dto);
        var opts = new Mock<IImportOptions>();

        var handler = new ImportDataCommandHandler(_importer.Object, _passwords.Object, _factory.Object, _ctx.Object, _logger.Object);

        // Act
        var selectedIds = new List<Guid> { childId };
        var cmd = new ImportDataCommand("c:\\file.pngx", opts.Object, selectedIds, importToSeparateFolder: false);
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _passwords.Verify(x => x.CreateAsync(It.Is<IEnumerable<PangoPassword>>(list => list.Count() == 2 && list.Any(p => p.Name == "ParentFolder")), _action.Object), Times.Once);
    }
}
