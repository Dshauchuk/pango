namespace Pango.Application.Common.Exceptions;

public class DataEncryptionException : PangoException
{
    public DataEncryptionException(string code, string message)
        : base(string.IsNullOrEmpty(code) ? ApplicationErrors.Data.EncryptionError : code, message)
    {
        
    }

    public DataEncryptionException(string code, string message, Exception inner)
        : base(code, message, inner)
    {

    }
}
