using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.Dialogs;

public interface IDialogService
{
    Task ShowAsync(IContentDialog dialog);

    Task<bool> ConfirmAsync(string confirmationTitle, string confirmationText);
}

