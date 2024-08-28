using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[AppView(Core.Enums.AppView.ExportImport)]
public sealed partial class ExportImportView : PageBase
{
    public ExportImportView()
    : base(App.Host.Services.GetRequiredService<ILogger<ExportImportView>>())
    {
        this.InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<ExportImportViewModel>();

        this.PasswordsTreeView.SelectionChanged += PasswordsTreeView_SelectionChanged;
    }

    private void PasswordsTreeView_SelectionChanged(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.Any())
        {
            foreach (var item in args.AddedItems)
            {
                if (item is PangoExplorerItem explorerItem)
                {
                    if (!explorerItem.IsSelected)
                    {
                        explorerItem.IsSelected = true;

                        if(explorerItem.Parent != null)
                        {
                            explorerItem.Parent.IsSelected = true;
                        }
                    }
                }
            }
        }

        if (args.RemovedItems.Any())
        {
            foreach (var item in args.RemovedItems)
            {
                if (item is PangoExplorerItem explorerItem)
                {
                    explorerItem.IsSelected = false;
                }
            }
        }
    }
}
