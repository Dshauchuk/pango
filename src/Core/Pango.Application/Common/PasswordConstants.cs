namespace Pango.Application.Common;

public class PasswordConstants
{
    public const int MinLength = 3;
    public const int MaxLength = 64;
    public const int SafeLength = 16;
    public const int WeakThreshold = 3;
    public const int MediumThreshold = 6;
    public const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    public const string Digits = "0123456789";
    public const string Special = "!@#$%^&*()-_=+[]{};:,.<>/?";
    public const string Ambiguous = "0O1Il|";
    public const string StaticExportSalt = "PangoStaticExportSalt";
}
