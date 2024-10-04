namespace Pango.Application.Common.Exceptions;


public class PangoDataDecryptionException : PangoException
{
    public PangoDataDecryptionException(string code, string message)
        : base(string.IsNullOrEmpty(code) ? ApplicationErrors.Data.DecryptionError : code, message)
    {

    }

    public PangoDataDecryptionException(string code, string message, Exception inner)
        : base(code, message, inner)
    {

    }
}