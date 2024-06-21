using Windows.ApplicationModel.Resources;
using Microsoft.UI.Xaml.Markup;

namespace Pango.Desktop.Uwp.Core.Localization;

[MarkupExtensionReturnType(ReturnType = typeof(string))]
public class StringResourceExtension : MarkupExtension
{
    private static readonly ResourceLoader _resourceLoader = new();

    public StringResourceExtension() { }

    public string Key { get; set; } = "";

    protected override object ProvideValue()
    {
        return _resourceLoader.GetString(Key);
    }
}
