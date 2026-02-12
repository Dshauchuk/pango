using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Domain.Entities;
using System.Globalization;

namespace Pango.Application.UseCases.Data.Commands.Import;

public class ImportDataCommandHandler(
    IDataImporter dataImporter,
    IPasswordRepository passwordRepository,
    IRepositoryContextFactory repositoryContextFactory,
    IUserContextProvider userContextProvider,
    ILogger<ImportDataCommandHandler> logger) : IRequestHandler<ImportDataCommand, ErrorOr<ImportResult>>
{
    public async Task<ErrorOr<ImportResult>> Handle(ImportDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            ImportResultDto result = await dataImporter.ImportAsync(request.SourcePath, request.Options);
            if (result is null) return Error.Failure(ApplicationErrors.Data.ImportError, "Import result is null");

            var context = repositoryContextFactory.Create(userContextProvider.GetUserName(), await userContextProvider.GetEncodingOptionsAsync());
            string? importRootFolder = null;

            // Import into separate folder (original behavior)
            if (request.ImportToSeparateFolder)
            {
                var culture = CultureInfo.CurrentUICulture.Name;
                string baseFolderName = culture.StartsWith("be", StringComparison.OrdinalIgnoreCase) ? "Імпартаванае" : "Imported";
                string finalFolderName = baseFolderName;
                var allRootFolders = (await passwordRepository.QueryAsync(p => p.IsCatalog && string.IsNullOrEmpty(p.CatalogPath), context)).Select(x => x.Name).ToList();
                int counter = 1;
                while (allRootFolders.Any(n => n.Equals(finalFolderName, StringComparison.OrdinalIgnoreCase)))
                {
                    finalFolderName = $"{baseFolderName} ({counter})";
                    counter++;
                }
                var rootFolderNode = new PangoPassword
                {
                    Id = Guid.NewGuid(),
                    Name = finalFolderName,
                    IsCatalog = true,
                    CatalogPath = string.Empty,
                    UserName = userContextProvider.GetUserName(),
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await passwordRepository.CreateAsync(rootFolderNode, context);
                importRootFolder = rootFolderNode.Name;
            }
            // Import into root catalog
            else
            {
                string? sourceRootName = null;
                foreach (IContentPackage package in result.ContentPackages)
                {
                    if (package.ContentType == Domain.Enums.ContentType.Passwords && package.Data is IEnumerable<PangoPassword> importedItems)
                    {
                        var rootItems = importedItems.Where(x => string.IsNullOrEmpty(x.CatalogPath)).ToList();
                        if (rootItems.Count != 0) { sourceRootName = rootItems.First().Name; break; }
                    }
                }

                if (!string.IsNullOrEmpty(sourceRootName))
                {
                    var allRootFolders = (await passwordRepository.QueryAsync(p => p.IsCatalog && string.IsNullOrEmpty(p.CatalogPath), context)).Select(x => x.Name).ToList();
                    string finalRootName = sourceRootName;
                    int counter = 1;
                    while (allRootFolders.Any(n => n.Equals(finalRootName, StringComparison.OrdinalIgnoreCase)))
                    {
                        finalRootName = $"{sourceRootName} ({counter})";
                        counter++;
                    }
                    var rootFolderNode = new PangoPassword
                    {
                        Id = Guid.NewGuid(),
                        Name = finalRootName,
                        IsCatalog = true,
                        CatalogPath = string.Empty,
                        UserName = userContextProvider.GetUserName(),
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    await passwordRepository.CreateAsync(rootFolderNode, context);
                    importRootFolder = finalRootName;
                }
            }

            List<PangoPassword> itemsToCreate = [];
            HashSet<string> importedCatalogsLookup = new(StringComparer.OrdinalIgnoreCase);

            foreach (IContentPackage package in result.ContentPackages)
            {
                if (package.ContentType == Domain.Enums.ContentType.Passwords && package.Data is IEnumerable<PangoPassword> importedItems)
                {
                    var allSourceList = importedItems.ToList();
                    HashSet<Guid> selectedIdsSet = request.SelectedIds?.Count > 0 ? GetSelectedIdsWithParents(allSourceList, [.. request.SelectedIds]) : [.. allSourceList.Select(x => x.Id)];
                    var itemsToProcess = allSourceList.Where(x => selectedIdsSet.Contains(x.Id)).ToList();
                    var processedItems = ReconstructHierarchy(itemsToProcess, importRootFolder);

                    foreach (var newItem in processedItems)
                    {
                        if (string.IsNullOrWhiteSpace(newItem.Name)) continue;
                        if (newItem.IsCatalog)
                        {
                            string catalogKey = GetUniqueCatalogKey(newItem);
                            if (importedCatalogsLookup.Contains(catalogKey)) continue;
                            importedCatalogsLookup.Add(catalogKey);
                        }
                        newItem.Id = Guid.NewGuid();
                        newItem.UserName = userContextProvider.GetUserName();
                        newItem.CreatedAt = DateTimeOffset.UtcNow;
                        itemsToCreate.Add(newItem);
                    }
                }
            }

            if (itemsToCreate.Count != 0) await passwordRepository.CreateAsync(itemsToCreate, context);
            return new ImportResult(result.Manifest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import failed");
            return Error.Failure(ApplicationErrors.Data.ImportError, ex.Message);
        }
    }

    /// <summary>
    /// Generates a unique catalog key using CatalogPath and Name, separated by '|'.
    /// </summary>
    private static string GetUniqueCatalogKey(PangoPassword item)
    {
        return $"{item.CatalogPath?.Trim() ?? string.Empty}|{item.Name?.Trim() ?? string.Empty}";
    }

    /// <summary>
    /// Returns all selected IDs and their parent IDs recursively up the hierarchy.
    /// </summary>
    private static HashSet<Guid> GetSelectedIdsWithParents(List<PangoPassword> allItems, HashSet<Guid> selectedIds)
    {
        var result = new HashSet<Guid>(selectedIds);
        var parentMap = BuildParentMap(allItems);
        foreach (var selectedId in selectedIds.ToList())
        {
            var currentId = selectedId;
            while (parentMap.TryGetValue(currentId, out var parentId) && parentId.HasValue)
            {
                if (!result.Contains(parentId.Value)) result.Add(parentId.Value);
                currentId = parentId.Value;
            }
        }
        return result;
    }

    /// <summary>
    /// Builds a parent-child mapping using CatalogPath to identify hierarchical relationships.
    /// </summary>
    private static Dictionary<Guid, Guid?> BuildParentMap(List<PangoPassword> allItems)
    {
        var parentMap = new Dictionary<Guid, Guid?>();
        var pathMap = new Dictionary<string, PangoPassword>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in allItems)
        {
            if (string.IsNullOrWhiteSpace(item.Name)) continue;
            string fullPath = GetFullPath(item);
            if (!pathMap.ContainsKey(fullPath)) pathMap[fullPath] = item;
        }
        foreach (var item in allItems)
        {
            if (string.IsNullOrEmpty(item.CatalogPath)) parentMap[item.Id] = null;
            else
            {
                var parentPath = item.CatalogPath;
                parentMap[item.Id] = pathMap.TryGetValue(parentPath, out var parent) ? parent.Id : (Guid?)null;
            }
        }
        return parentMap;
    }

    /// <summary>
    /// Reconstructs the item hierarchy by prepending a forced root path to CatalogPath.
    /// </summary>
    private static List<PangoPassword> ReconstructHierarchy(List<PangoPassword> itemsToSave, string? forcedRoot)
    {
        var result = new List<PangoPassword>();
        foreach (var item in itemsToSave)
        {
            var newItem = item.Adapt<PangoPassword>();
            if (!string.IsNullOrEmpty(forcedRoot))
            {
                newItem.CatalogPath = string.IsNullOrEmpty(item.CatalogPath) ? forcedRoot : $"{forcedRoot}{AppConstants.CatalogDelimeter}{item.CatalogPath}";
            }
            else
            {
                newItem.CatalogPath = item.CatalogPath;
            }
            result.Add(newItem);
        }
        return result;
    }

    /// <summary>
    /// Constructs the full path of an item by combining CatalogPath and Name.
    /// </summary>
    private static string GetFullPath(PangoPassword item)
    {
        return string.IsNullOrEmpty(item.CatalogPath) ? item.Name : $"{item.CatalogPath}{AppConstants.CatalogDelimeter}{item.Name}";
    }
}
