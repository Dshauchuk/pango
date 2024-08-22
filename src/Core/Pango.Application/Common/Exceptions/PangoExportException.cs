namespace Pango.Application.Common.Exceptions;

/// <summary>
/// Thrown if an error occurred while exporting data
/// </summary>
[Serializable]
public class PangoExportException : PangoException
{
    public PangoExportException(string message) : base(ApplicationErrors.Data.ExportError, message)
    {

    }

    public PangoExportException(string message, Exception ex) : base(ApplicationErrors.Data.ExportError, message, ex)
    {
        
    }
}
