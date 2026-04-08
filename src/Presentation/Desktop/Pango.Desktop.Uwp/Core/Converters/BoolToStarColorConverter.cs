using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Pango.Desktop.Uwp.Core.Converters;

public class BoolToStarColorConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, string l)
        => (bool)value
            ? new SolidColorBrush(Microsoft.UI.Colors.Gold)
            : new SolidColorBrush(Microsoft.UI.Colors.Gray);

    public object ConvertBack(object value, Type t, object p, string l)
        => throw new NotImplementedException();
}