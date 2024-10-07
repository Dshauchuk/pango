using Pango.Application.Common;
using Pango.Desktop.Uwp.Models;

namespace Pango.Desktop.Uwp.Core.Extensions;

internal static class PangoExplorerItemExtensions
{
    /// <summary>
    /// Recalculates <see cref="PangoExplorerItem.CatalogPath"/> field, based on the <see cref="PangoExplorerItem.Parent"/> for passed <paramref name="item"/> and all its Children
    /// </summary>
    /// <param name="item">Element, for which and for which Children <see cref="PangoExplorerItem.CatalogPath"/> should be recalculated</param>
    internal static void RecalculateCatalogPath(this PangoExplorerItem item)
    {
        item.CatalogPath = item.Parent is null ? string.Empty : $"{item.Parent.CatalogPath}{AppConstants.CatalogDelimeter}{item.Parent.Name}";

        if (item.Type == PangoExplorerItem.ExplorerItemType.Folder)
        {
            foreach (PangoExplorerItem child in item.Children)
            {
                RecalculateCatalogPath(child);
            }
        }
    }
}
