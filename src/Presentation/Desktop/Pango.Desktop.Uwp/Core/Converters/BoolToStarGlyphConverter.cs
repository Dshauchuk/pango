using Microsoft.UI.Xaml.Data;
using System;

namespace Pango.Desktop.Uwp.Core.Converters;

public class BoolToStarGlyphConverter : IValueConverter
{
    // E735 = color star, E734 = empty star
    public object Convert(object value, Type t, object p, string l)
        => (bool)value ? "\uE735" : "\uE734";

    public object ConvertBack(object value, Type t, object p, string l)
        => throw new NotImplementedException();
}