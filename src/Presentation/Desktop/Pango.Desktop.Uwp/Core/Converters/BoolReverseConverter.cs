using Microsoft.UI.Xaml.Data;
using System;

namespace Pango.Desktop.Uwp.Core.Converters
{
    public class BoolReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
