using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Models;

namespace Pango.Desktop.Uwp.Controls.TemplateSelectors;

class PasswordExplorerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var explorerItem = (PangoExplorerItem)item;
        return (explorerItem.Type == PangoExplorerItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate) ?? throw new System.Exception("PasswordExplorerItemTemplateSelector: missing template");
    }
}
