namespace Pango.Application.Common;

public static class ApplicationErrors
{
    public static class Data
    {
        public const string EncryptionError = "Data.EncryptionError";
        public const string CannotDefineContentType = "Data.CannotDefineContentType";
        public const string UnkownError = "Data.UnkownError";
        public const string ExportError = "Data.ExportFailed";
    }

    public static class User 
    {
        public const string UnkownError = "User.UnkownError";
        public const string LoginFailed = "User.LoginFailed";
        public const string DeletionFailed = "User.DeletionFailed";
        public const string RegistrationFailed = "User.RegistrationFailed";
        public const string NotFound = "User.NotFound";
    }

    public static class Password
    {
        public const string QueryFailed = "Password.QueryFailed";
        public const string NotFound = "Password.NotFound";
        public const string DeletionFailed = "Password.DeletionFailed";
        public const string CreationFailed = "Password.DeletionFailed";
        public const string ModificationFailed = "Password.ModificationFailed";
    }
}
