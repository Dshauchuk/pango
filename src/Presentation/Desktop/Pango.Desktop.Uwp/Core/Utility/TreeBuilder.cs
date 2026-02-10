using Pango.Application.Common;
using Pango.Desktop.Uwp.Models;
using Pango.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pango.Desktop.Uwp.Core.Utility;

public static class TreeBuilder
{
    // Constructs the hierarchical tree from a flat list of passwords.
    public static ObservableCollection<PangoExplorerItem> BuildTree(List<PangoPassword> flatList)
    {
        var validItems = flatList.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
        var folders = validItems.Where(x => x.IsCatalog).ToList();
        var files = validItems.Where(x => !x.IsCatalog).ToList();

        var folderMap = new Dictionary<string, PangoExplorerItem>(StringComparer.OrdinalIgnoreCase);

        // create folder nodes
        foreach (var f in folders)
        {
            string fullPath = GetEntityFullPath(f);
            if (!folderMap.ContainsKey(fullPath))
            {
                folderMap[fullPath] = new PangoExplorerItem(f.Id, f.Name, PangoExplorerItem.ExplorerItemType.Folder)
                {
                    CatalogPath = f.CatalogPath?.Trim() ?? string.Empty,
                    IsSelected = false
                };
            }
        }

        var rootItems = new ObservableCollection<PangoExplorerItem>();
        var allItemsToProcess = new List<PangoExplorerItem>(folderMap.Values);

        // create file nodes
        foreach (var f in files)
        {
            allItemsToProcess.Add(new PangoExplorerItem(f.Id, f.Name, PangoExplorerItem.ExplorerItemType.File)
            {
                CatalogPath = f.CatalogPath?.Trim() ?? string.Empty,
                IsSelected = false
            });
        }

        // link items to parents
        foreach (var item in allItemsToProcess)
        {
            if (string.IsNullOrEmpty(item.CatalogPath))
            {
                rootItems.Add(item);
            }
            else
            {
                if (folderMap.TryGetValue(item.CatalogPath, out var parentFolder))
                {
                    parentFolder.AddChild(item);
                }
                else
                {
                }
            }
        }

        SortChildren(rootItems);
        return rootItems;
    }

    // Recursively sorts the tree structure.
    private static void SortChildren(ObservableCollection<PangoExplorerItem> items)
    {
        var sorted = items.OrderByDescending(x => x.Type == PangoExplorerItem.ExplorerItemType.Folder)
                          .ThenBy(x => x.Name)
                          .ToList();
        items.Clear();
        foreach (var item in sorted)
        {
            if (item.Children.Count > 0) SortChildren(item.Children);
            items.Add(item);
        }
    }

    // Helper to get full path string.
    private static string GetEntityFullPath(PangoPassword item)
    {
        if (string.IsNullOrEmpty(item.CatalogPath)) return item.Name;
        return $"{item.CatalogPath}{AppConstants.CatalogDelimeter}{item.Name}";
    }
}