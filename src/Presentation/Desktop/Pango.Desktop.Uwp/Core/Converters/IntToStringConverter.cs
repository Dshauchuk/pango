using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Pango.Desktop.Uwp.Core.Converters;

public sealed class IntToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value?.ToString() ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var text = value as string;

        if (int.TryParse(text, out var parsed))
        {
            return parsed;
        }

        // letters - don't touch vm
        return DependencyProperty.UnsetValue;
    }
}

