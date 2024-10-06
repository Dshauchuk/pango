namespace Pango.Application.Common.Exceptions;

public class PangoDataEncryptionException : PangoException
{
    public PangoDataEncryptionException(string code, string message)
        : base(string.IsNullOrEmpty(code) ? ApplicationErrors.Data.EncryptionError : code, message)
    {
        
    }

    public PangoDataEncryptionException(string code, string message, Exception inner)
        : base(code, message, inner)
    {

    }
}
