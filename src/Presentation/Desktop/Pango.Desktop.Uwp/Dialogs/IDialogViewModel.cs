using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs;

public interface IDialogViewModel
{
    Task OnSaveAsync();
    Task OnCancelAsync();
}
