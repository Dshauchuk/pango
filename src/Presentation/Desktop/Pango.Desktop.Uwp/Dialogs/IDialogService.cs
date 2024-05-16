using Pango.Desktop.Uwp.Dialogs.Parameters;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs;

public interface IDialogService
{
    Task ShowNewCatalogDialog(EditCatalogParameters catalogParameters);

    Task<bool> ConfirmAsync(string confirmationTitle, string confirmationText);
}

