namespace Pango.Application.Common.Interfaces;

public interface IFileOptions
{
    int PasswordsPerFile { get; set; }
}

public interface IAppOptions
{
    IFileOptions FileOptions { get; set; }   
}
