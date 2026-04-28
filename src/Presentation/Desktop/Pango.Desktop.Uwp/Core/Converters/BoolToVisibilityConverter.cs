using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Pango.Desktop.Uwp.Core.Converters;

public partial class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isReversed = parameter != null && (parameter.ToString()?.ToUpper().Equals("REVERSE") ?? false);
        bool boolVal = (bool?)value ?? false;

        return boolVal ^ isReversed ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
