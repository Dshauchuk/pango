using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.ViewModels;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public class ExportDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields

    private readonly ISender _sender;

    #endregion

    public ExportDialogViewModel(ILogger<ExportDialogViewModel> logger, ISender sender) 
        : base(logger)
    {
        DialogContext = new DialogContext();
        _sender = sender;
    }

    public IDialogContext DialogContext { get; }

    public bool CanSave()
    {
        return true;
    }

    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnSaveAsync()
    {
        return Task.CompletedTask;
    }
}
