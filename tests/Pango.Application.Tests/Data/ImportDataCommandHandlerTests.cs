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
        var manifest = new PangoPackageManifest("bob", DateTime.UtcNow.ToString("G"), "d", new Dictionary<ContentType, int>());
        var packages = new List<IContentPackage>
        {
            new ContentPackage("bob", ContentType.Passwords, typeof(List<PangoPassword>).FullName!, 0, new List<PangoPassword>(), DateTimeOffset.UtcNow),
        };
        var dto = new ImportResultDto(manifest, packages);

        _importer.Setup(x => x.ImportAsync("c:\\a.pngx", It.IsAny<IImportOptions>())).ReturnsAsync(dto);

        _passwords.Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(Array.Empty<PangoPassword>());
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
}
