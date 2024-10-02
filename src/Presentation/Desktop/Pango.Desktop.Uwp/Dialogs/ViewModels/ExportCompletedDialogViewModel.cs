using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.ViewModels;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public class ExportCompletedDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields

    private string _folderPath = string.Empty;
    private string _fileName = string.Empty;
    private string _appVersion = string.Empty;
    private string _contentsInfo = string.Empty;
    private string _generatedAt = string.Empty;

    #endregion

    public ExportCompletedDialogViewModel(ILogger<ExportCompletedDialogViewModel> logger) : base(logger)
    {
        DialogContext = new DialogContext();
    }

    #region Properties

    public IDialogContext DialogContext { get; }

    public string AppVersion
    {
        get => _appVersion;
        set => SetProperty(ref _appVersion, value);
    }

    public string FolderPath
    {
        get => _folderPath;
        set => SetProperty(ref _folderPath, value);
    }

    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    public string ContentsInfo
    {
        get => _contentsInfo;
        set => SetProperty(ref _contentsInfo, value);
    }

    public string GeneratedAt
    {
        get => _generatedAt;
        set => SetProperty(ref _generatedAt, value);
    }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        if(parameter is not ExportResultParameters exportResultParameters)
        {
            return;
        }

        AppVersion = exportResultParameters.ExportResult.AppVersion;
        FileName = Path.GetFileName(exportResultParameters.ExportResult.Path);
        FolderPath = Path.GetDirectoryName(exportResultParameters.ExportResult.Path);
        ContentsInfo = $"{exportResultParameters.ExportResult.Contents.Values.First()} password(s) exoprted";
        GeneratedAt = exportResultParameters.ExportResult.GeneratedAt.ToString("G");
    }

    #endregion

    #region Public Methods

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

    #endregion
}
