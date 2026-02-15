namespace Pango.Application.Common;

public sealed record PasswordGenerationOptions(
    int Length,
    bool UseUppercase,
    bool UseLowercase,
    bool UseDigits,
    bool UseSpecial,
    bool ExcludeAmbiguous);

