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
    public static List<PasswordExplorerItem> FindItems(this IEnumerable<PasswordExplorerItem> items, Func<PasswordExplorerItem, bool> predicate)
    {
        List<PasswordExplorerItem> resultList = [];

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
                List<PasswordExplorerItem> findings = FindItems(item.Children, predicate);

                if (findings.Any())
                {
                    resultList.AddRange(findings);
                }
            }
        }

        return resultList;
    }
}
