namespace Pango.Application.Common;

public static class ApplicationErrors
{
    public static class Data
    {
        public const string EncryptionError = "Data.EncryptionError";
        public const string DecryptionError = "Data.DecryptionError";
        public const string CannotDefineContentType = "Data.CannotDefineContentType";
        public const string UnknownError = "Data.UnknownError";
        public const string ExportError = "Data.ExportFailed";
        public const string ImportError = "Data.ImportFailed";
    }

    public static class User 
    {
        public const string UnknownError = "User.UnknownError";
        public const string LoginFailed = "User.LoginFailed";
        public const string DeletionFailed = "User.DeletionFailed";
        public const string RegistrationFailed = "User.RegistrationFailed";
        public const string NotFound = "User.NotFound";
        public const string TooManyUsers = "User.TooManyUsers";
        public const string ChangePasswordFailed = "User.ChangePasswordFailed";
    }

    public static class Password
    {
        public const string QueryFailed = "Password.QueryFailed";
        public const string NotFound = "Password.NotFound";
        public const string DeletionFailed = "Password.DeletionFailed";
        public const string CreationFailed = "Password.CreationFailed";
        public const string ModificationFailed = "Password.ModificationFailed";
        public const string GenerationInvalidLength = "Password.Generation.InvalidLength";
        public const string GenerationInvalidCharsets = "Password.Generation.InvalidCharsets";
    }
}
