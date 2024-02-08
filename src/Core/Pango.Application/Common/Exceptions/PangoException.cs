namespace Pango.Application.Common.Exceptions;

[Serializable]
public class PangoException : Exception
{
    public PangoException(string code)
    {
        Code = code;
    }

    public PangoException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public PangoException(string code, string message, Exception inner)
        : base(message, inner)
    {
        Code = code;
    }

    public string Code { get; private set; }
}
