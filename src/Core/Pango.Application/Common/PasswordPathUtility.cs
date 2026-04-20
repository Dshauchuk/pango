namespace Pango.Application.Common;

/// <summary>
/// Utility class for working with password catalog paths.
/// </summary>
public static class PasswordPathUtility
{
    /// <summary>
    /// Builds a catalog path by joining non-empty path segments with the configured delimiter.
    /// </summary>
    /// <param name="paths">Path segments to join.</param>
    /// <returns>A concatenated catalog path, or an empty string if no valid segments provided.</returns>
    public static string BuildCatalogPath(params string[] paths)
    {
        if (paths is null || paths.Length == 0)
        {
            return string.Empty;
        }

        paths = [.. paths.Where(p => !string.IsNullOrWhiteSpace(p))];

        return string.Join(AppConstants.CatalogDelimeter, paths);
    }

    /// <summary>
    /// Compares two catalog paths for equality, ignoring leading slashes.
    /// </summary>
    /// <param name="path1">The first path to compare.</param>
    /// <param name="path2">The second path to compare.</param>
    /// <returns><c>true</c> if both paths are equal after normalization; otherwise, <c>false</c>.</returns>
    public static bool AreEqual(string? path1, string? path2)
    {
        if (string.IsNullOrWhiteSpace(path1) && string.IsNullOrWhiteSpace(path2))
        {
            return true;
        }

        path1 = NormalizePath(path1);
        path2 = NormalizePath(path2);

        return path1.Equals(path2, StringComparison.Ordinal);
    }

    /// <summary>
    /// Removes a leading slash from the path if present.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path without a leading slash, or empty string if input was null.</returns>
    private static string NormalizePath(string? path) =>
        path is not null && path.StartsWith('/')
            ? path[1..]
            : path ?? string.Empty;
}