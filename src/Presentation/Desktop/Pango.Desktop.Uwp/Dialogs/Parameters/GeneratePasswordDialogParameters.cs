namespace Pango.Desktop.Uwp.Dialogs.Parameters;

public class GeneratePasswordDialogParameters(string password) : IDialogParameter
{
    public string GeneratedPassword { get; } = password;
}

