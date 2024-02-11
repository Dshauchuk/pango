namespace Pango.Application.Common.Exceptions;

public class PasswordNotFoundException : PangoException
{
    public PasswordNotFoundException(string message) : base(ApplicationErrors.Password.NotFound, message)
    {
    }
}
