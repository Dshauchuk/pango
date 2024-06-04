namespace Pango.Application.Models;

public class PangoUserDto : DtoBase
{
    public string? UserName { get; set; }

    public string? MasterPasswordHash { get; set; }
 
    public string? PasswordSalt { get; set; }
}
