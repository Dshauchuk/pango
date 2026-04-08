using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Pango.Desktop.Uwp.Core.Enums;
using System;

namespace Pango.Desktop.Uwp.Core.Converters;

/// <summary>
/// Converts expiration status to visibility based on provided parameter
/// </summary>
public partial class ExpirationStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is PasswordExpirationStatus status && parameter is string targetStatusStr)
        {
            if (Enum.TryParse<PasswordExpirationStatus>(targetStatusStr, out var targetStatus))
            {
                return status == targetStatus ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}