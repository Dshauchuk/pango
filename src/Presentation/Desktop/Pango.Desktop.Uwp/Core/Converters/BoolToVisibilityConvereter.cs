using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Pango.Desktop.Uwp.Core.Converters;

public class BoolToVisibilityConvereter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isReversed = parameter != null && parameter.ToString().ToUpper().Equals("REVERSE");
        bool boolVal = (bool?)value ?? false;

        return boolVal ^ isReversed ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
