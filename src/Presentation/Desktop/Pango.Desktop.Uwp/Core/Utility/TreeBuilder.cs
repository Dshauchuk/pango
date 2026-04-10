using Pango.Application.Common;
using Pango.Desktop.Uwp.Models;
using Pango.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Core.Utility;

/// <summary>
/// Utility class to build the hierarchical folder structure for passwords.
/// </summary>
public static class TreeBuilder
{
    /// <summary>
    /// Asynchronously builds the visual tree structure from a flat list of passwords.
    /// </summary>
    /// <param name="flatList">The flat list of PangoPassword items.</param>
    /// <param name="previouslyExpandedPaths">A set of folder paths that were expanded before the refresh.</param>
    /// <returns>A hierarchical ObservableCollection ready for TreeView binding.</returns>
    public static async Task<ObservableCollection<PangoExplorerItem>> BuildTreeAsync(
        List<PangoPassword> flatList,
        HashSet<string>? previouslyExpandedPaths = null)
    {
        return await Task.Run(() =>
        {
            var folderMap = new Dictionary<string, PangoExplorerItem>(StringComparer.OrdinalIgnoreCase);
            var childrenMap = new Dictionary<string, List<PangoExplorerItem>>(StringComparer.OrdinalIgnoreCase);
            var rootItemsList = new List<PangoExplorerItem>();

            foreach (var item in flatList)
            {
                if (string.IsNullOrWhiteSpace(item.Name)) continue;
                string catalogPath = item.CatalogPath?.Trim() ?? string.Empty;

                if (item.IsCatalog)
                {
                    string fullPath = string.IsNullOrEmpty(catalogPath) ? item.Name : $"{catalogPath}{AppConstants.CatalogDelimeter}{item.Name}";
                    if (!folderMap.ContainsKey(fullPath))
                    {
                        bool isExpanded = previouslyExpandedPaths != null && previouslyExpandedPaths.Contains(fullPath);
                        folderMap[fullPath] = new PangoExplorerItem(item.Id, item.Name, PangoExplorerItem.ExplorerItemType.Folder)
                        {
                            CatalogPath = catalogPath,
                            IsSelected = false,
                            IsExpanded = isExpanded
                        };
                        childrenMap[fullPath] = [];
                    }
                }
            }

            foreach (var item in flatList)
            {
                if (string.IsNullOrWhiteSpace(item.Name)) continue;
                string catalogPath = item.CatalogPath?.Trim() ?? string.Empty;

                PangoExplorerItem explorerItem;
                if (item.IsCatalog)
                {
                    string fullPath = string.IsNullOrEmpty(catalogPath) ? item.Name : $"{catalogPath}{AppConstants.CatalogDelimeter}{item.Name}";
                    explorerItem = folderMap[fullPath];
                }
                else
                {
                    explorerItem = new PangoExplorerItem(item.Id, item.Name, PangoExplorerItem.ExplorerItemType.File)
                    {
                        CatalogPath = catalogPath,
                        IsSelected = false
                    };
                }

                if (string.IsNullOrEmpty(catalogPath))
                {
                    if (!item.IsCatalog || !rootItemsList.Contains(explorerItem))
                        rootItemsList.Add(explorerItem);
                }
                else
                {
                    if (childrenMap.TryGetValue(catalogPath, out var list))
                    {
                        if (!item.IsCatalog || !list.Contains(explorerItem))
                            list.Add(explorerItem);
                    }
                    else
                    {
                        if (!item.IsCatalog || !rootItemsList.Contains(explorerItem))
                            rootItemsList.Add(explorerItem);
                    }
                }
            }

            SortAndBind(rootItemsList, childrenMap, null);
            return new ObservableCollection<PangoExplorerItem>(rootItemsList);
        });
    }

    /// <summary>
    /// Recursively sorts the tree structure and binds parents.
    /// </summary>
    private static void SortAndBind(List<PangoExplorerItem> items, Dictionary<string, List<PangoExplorerItem>> childrenMap, PangoExplorerItem? parent)
    {
        items.Sort((a, b) =>
        {
            if (a.Type != b.Type) return a.Type == PangoExplorerItem.ExplorerItemType.Folder ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var item in items)
        {
            item.Parent = parent;
            if (item.Type == PangoExplorerItem.ExplorerItemType.Folder)
            {
                string fullPath = string.IsNullOrEmpty(item.CatalogPath) ? item.Name : $"{item.CatalogPath}{AppConstants.CatalogDelimeter}{item.Name}";
                if (childrenMap.TryGetValue(fullPath, out var children))
                {
                    SortAndBind(children, childrenMap, item);
                    item.Children = new ObservableCollection<PangoExplorerItem>(children);
                }
            }
        }
    }
}