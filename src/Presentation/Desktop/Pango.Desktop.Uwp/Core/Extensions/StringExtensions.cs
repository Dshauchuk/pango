using System.Collections.Generic;
using System.Linq;

namespace Pango.Desktop.Uwp.Core.Extensions;

public static class StringExtensions
{
    public static Queue<string> ParseCatalogPath(this string catalogPath)
    {
        var pathSegments = catalogPath?.Split('/').Where(s => !string.IsNullOrEmpty(s)) ?? [];

        return new Queue<string>(pathSegments);
    }
}
