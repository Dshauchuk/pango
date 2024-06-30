using System;
using Microsoft.UI.Xaml.Data;

namespace Pango.Desktop.Uwp.Core.Converters;

public class EmptyStringToRootFolderNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if(string.IsNullOrEmpty(value as string))
        {
            return "...";
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
