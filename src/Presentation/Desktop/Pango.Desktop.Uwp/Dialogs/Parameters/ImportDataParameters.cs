namespace Pango.Desktop.Uwp.Dialogs.Parameters;

public class ImportDataParameters(string filePath) : IDialogParameter
{
    public string FilePath { get; } = filePath;
}
