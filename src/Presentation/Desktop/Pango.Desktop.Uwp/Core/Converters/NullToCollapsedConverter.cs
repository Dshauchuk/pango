using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Pango.Desktop.Uwp.Core.Converters;

public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
