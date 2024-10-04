using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Extensions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;
using Pango.Domain.Enums;

namespace Pango.Application.UseCases.Data.Commands.Export;

public class ExportDataCommandHandler
    : IRequestHandler<ExportDataCommand, ErrorOr<ExportResult>>
{
    private readonly ILogger _logger; 
    private readonly IDataExporter _dataExporter;
    private readonly IPasswordRepository _passwordRepository;
    private readonly IAppMetaService _appMetaService;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory;

    public ExportDataCommandHandler(
        ILogger<ExportDataCommandHandler> logger,
        IDataExporter dataExporter,
        IPasswordRepository passwordRepository,
        IRepositoryContextFactory repositoryContextFactory,
        IUserContextProvider userContextProvider,
        IAppMetaService appMetaService)
    {
        _logger = logger;
        _dataExporter = dataExporter;
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _repositoryContextFactory = repositoryContextFactory;
        _appMetaService = appMetaService;
    }

    public async Task<ErrorOr<ExportResult>> Handle(ExportDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Guid[] ids = request.ExportItems.Select(i => i.Id).ToArray();
            IRepositoryActionContext context = _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync());

            List<PangoPassword> passwords = (await _passwordRepository.QueryAsync(p => ids.Contains(p.Id), context)).ToList();

            var manifest = 
                new PangoPackageManifest(
                    _userContextProvider.GetUserName(), 
                    DateTime.UtcNow.ToString("G"), 
                    request.ExportOptions.Description, 
                    new Dictionary<Domain.Enums.ContentType, int>() { { ContentType.Passwords, passwords.Count(p => !p.IsCatalog)} }
                    );

            string path = await _dataExporter.ExportAsync(manifest, ExportDataCommandHandler.AsContent(passwords, request.ExportOptions.Description), request.ExportOptions);
        
            return new ExportResult(
                path, 
                new Dictionary<Domain.Enums.ContentType, int> { { Domain.Enums.ContentType.Passwords, passwords.Where(p => !p.IsCatalog).Count() } }, 
                DateTime.Now, 
                _appMetaService.GetAppVersion());
        }
        catch(PangoExportException pEx)
        {
            _logger.LogError(pEx, "Data export failed: {message}", pEx.Message);
            return Error.Failure(pEx.Code, pEx.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data export failed: {message}", ex.Message);
            return Error.Failure(ApplicationErrors.Data.ExportError, $"Export failed: {ex.Message}");
        }
    }

    private static List<IContentPackage> AsContent(List<PangoPassword> passwords, string owner)
    {
        List<IContentPackage> fileContents = new(100);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var chunk in passwords.ChunkBy(2))
        {
            ContentPackage fileContent = new(owner, Domain.Enums.ContentType.Passwords, chunk.GetType().FullName ?? string.Empty, chunk.Count, chunk, now);
            fileContents.Add(fileContent);
        }

        return fileContents;
    }
}
