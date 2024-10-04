using Pango.Desktop.Uwp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pango.Desktop.Uwp.Core.Extensions;

public static class CollectionExtensions
{
    /// <summary>
    /// Returns <see cref="List{PasswordExplorerItem}"/> that fit <paramref name="predicate"/>
    /// </summary>
    /// <param name="items"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public static List<PangoExplorerItem> FindItems(this IEnumerable<PangoExplorerItem> items, Func<PangoExplorerItem, bool> predicate)
    {
        List<PangoExplorerItem> resultList = [];

        if (items == null || !items.Any())
        {
            return resultList;
        }

        foreach (var item in items)
        {
            if (predicate(item))
            {
                resultList.Add(item);
            }

            if (item.Children.Any())
            {
                List<PangoExplorerItem> findings = FindItems(item.Children, predicate);

                if (findings.Any())
                {
                    resultList.AddRange(findings);
                }
            }
        }

        return resultList;
    }
}
