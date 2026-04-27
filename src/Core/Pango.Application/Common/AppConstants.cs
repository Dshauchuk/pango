namespace Pango.Application.Common;

public static class AppConstants
{
    /// <summary>
    /// Presents a default number of items stored in each file
    /// </summary>
    public const int DefaultNumberOfItemsPerFile = 20;

    public const char CatalogDelimeter = '/';
    /// <summary>
    /// File extension used for exported data files.
    /// </summary>
    public const string ExportFileExtension = ".pngx";
    /// <summary>
    /// Default name of the subfolder used to store exported files
    /// under the user's Documents directory.
    /// </summary>
    public const string DefaultExportFolderName = "PangoExports";

    public const string UsersFolderName = "users";

    public static class Security
    {
        public const int FileStreamBufferSize = 4096;
        public const int MemoryStreamDefaultCapacity = 32768;

        // Key derivation
        public const int Pbkdf2Iterations = 50000;
        public const int Pbkdf2HashIterations = 10000;

        // AES Configuration
        public const int AesKeySize = 32;
        public const int AesIvSize = 16;
        public const int DerivationOutputSize = 48; // 32 key + 16 iv
    }
}
