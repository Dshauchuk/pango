using System;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs;

public interface IDialogContext
{
    event EventHandler OnContentChanged;
    void RaiseDialogContentChanged(object? sender = null);
}

public class DialogContext : IDialogContext
{
    public event EventHandler? OnContentChanged;

    public void RaiseDialogContentChanged(object? sender = null)
    {
        OnContentChanged?.Invoke(sender, EventArgs.Empty);
    }
}

public interface IDialogViewModel
{
    IDialogContext DialogContext { get; }
    Task OnSaveAsync();
    bool CanSave();
    Task OnCancelAsync();
}
