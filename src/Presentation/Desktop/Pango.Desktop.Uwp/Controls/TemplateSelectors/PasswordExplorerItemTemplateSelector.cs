using Pango.Desktop.Uwp.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.Controls.TemplateSelectors;

class PasswordExplorerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate FolderTemplate { get; set; }
    public DataTemplate FileTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var explorerItem = (PasswordExplorerItem)item;
        return explorerItem.Type == PasswordExplorerItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate;
    }
}
