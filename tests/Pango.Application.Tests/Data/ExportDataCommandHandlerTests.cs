using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Data.Commands.Export;
using Pango.Domain.Entities;
using Pango.Domain.Enums;

namespace Pango.Application.Tests.Data;

public class ExportDataCommandHandlerTests
{
    private readonly Mock<IDataExporter> _exporter = new();
    private readonly Mock<IPasswordRepository> _passwords = new();
    private readonly Mock<IUserContextProvider> _ctx = new();
    private readonly Mock<IRepositoryContextFactory> _factory = new();
    private readonly Mock<IRepositoryActionContext> _action = new();
    private readonly Mock<IAppMetaService> _meta = new();
    private readonly Mock<ILogger<ExportDataCommandHandler>> _logger = new();

    public ExportDataCommandHandlerTests()
    {
        _ctx.Setup(x => x.GetUserName()).Returns("alice");
        _ctx.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k", "s"));
        _factory.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>())).Returns(_action.Object);
        _meta.Setup(x => x.GetAppVersion()).Returns("1.0.0");
    }

    [Fact]
    public async Task Handle_CallsExporter_AndReturnsResult()
    {
        var id = Guid.NewGuid();
        var list = new List<PangoPassword>
        {
            new() { Id = id, Name = "A", IsCatalog = false },
            new() { Id = Guid.NewGuid(), Name = "Folder", IsCatalog = true },
        };
        _passwords.Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(list);

        var opts = new Mock<IExportOptions>();
        opts.Setup(o => o.Description).Returns("backup");
        opts.Setup(o => o.EncodingOptions).Returns(new EncodingOptions("k", "s"));

        _exporter.Setup(x => x.ExportAsync(It.IsAny<PangoPackageManifest>(), It.IsAny<IEnumerable<IContentPackage>>(), opts.Object))
            .ReturnsAsync(@"C:\out\file.pngx");

        var handler = new ExportDataCommandHandler(_logger.Object, _exporter.Object, _passwords.Object, _factory.Object, _ctx.Object, _meta.Object);
        var cmd = new ExportDataCommand(
            new List<ExportItem> { new(ContentType.Passwords, id) },
            opts.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(@"C:\out\file.pngx", result.Value.Path);
        Assert.Equal("1.0.0", result.Value.AppVersion);
        Assert.Equal(1, result.Value.Contents[ContentType.Passwords]);
    }

    [Fact]
    public async Task Handle_ReturnsPangoExportError_WhenExporterThrowsPangoExport()
    {
        _passwords.Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(Array.Empty<PangoPassword>());

        var opts = new Mock<IExportOptions>();
        opts.Setup(o => o.Description).Returns("d");
        opts.Setup(o => o.EncodingOptions).Returns(new EncodingOptions("k", "s"));

        _exporter.Setup(x => x.ExportAsync(It.IsAny<PangoPackageManifest>(), It.IsAny<IEnumerable<IContentPackage>>(), opts.Object))
            .ThrowsAsync(new PangoExportException("bad"));

        var handler = new ExportDataCommandHandler(_logger.Object, _exporter.Object, _passwords.Object, _factory.Object, _ctx.Object, _meta.Object);
        var result = await handler.Handle(new ExportDataCommand(new List<ExportItem>(), opts.Object), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Data.ExportError, result.FirstError.Code);
    }
}
