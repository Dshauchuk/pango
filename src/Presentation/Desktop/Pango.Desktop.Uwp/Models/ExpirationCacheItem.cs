using System;

namespace Pango.Desktop.Uwp.Models
{
    public class ExpirationCacheItem
    {
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset Date { get; set; }
    }
}