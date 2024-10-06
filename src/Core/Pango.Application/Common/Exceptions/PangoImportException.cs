namespace Pango.Application.Common.Exceptions;

/// <summary>
/// Thrown if an error occurred while importing data into the app
/// </summary>
[Serializable]
public class PangoImportException : PangoException
{
    public PangoImportException(string message) : base(ApplicationErrors.Data.ImportError, message)
    {

    }

    public PangoImportException(string message, Exception ex) : base(ApplicationErrors.Data.ImportError, message, ex)
    {

    }
}
