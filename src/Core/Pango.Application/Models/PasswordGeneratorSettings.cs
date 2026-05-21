using Pango.Application.Common;

namespace Pango.Application.Models;

public class PasswordGeneratorSettings
{
    public int Length { get; set; } = PasswordConstants.SafeLength;
    public bool UseUppercase { get; set; } = true;
    public bool UseLowercase { get; set; } = true;
    public bool UseDigits { get; set; } = true;
    public bool UseSpecial { get; set; } = false;
    public bool ExcludeAmbiguous { get; set; } = false;

}