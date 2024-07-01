using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace Pango.Desktop.Uwp.Core.Utility;

public static class VisualTreeHelperExtensions
{
    public static List<UIElement> GetChildUIElements(DependencyObject parent)
    {
        List<UIElement> result = new List<UIElement>();
        GetChildUIElements(parent, result);
        return result;
    }

    private static void GetChildUIElements(DependencyObject parent, List<UIElement> result)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is UIElement element)
            {
                result.Add(element);
            }
            GetChildUIElements(child, result); // Recursively get children
        }
    }
}
