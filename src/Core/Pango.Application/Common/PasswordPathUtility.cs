namespace Pango.Application.Common;

public static class PasswordPathUtility
{
    /// <summary>
    /// Returns a catalog path build based on <paramref name="paths"/>
    /// </summary>
    /// <param name="paths">path segments</param>
    /// <returns></returns>
    public static string BuildCatalogPath(params string[] paths)
    {
        if(paths is null || paths.Length == 0)
        {
            return string.Empty;
        }

        paths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        return string.Join(AppConstants.CatalogDelimeter, paths);
    }

    /// <summary>
    /// Returns true if <paramref name="path1"/> equals <paramref name="path2"/>, otherwise - false
    /// </summary>
    /// <param name="path1"></param>
    /// <param name="path2"></param>
    /// <returns></returns>
    public static bool AreEqual(string path1, string path2)
    {
        if(string.IsNullOrWhiteSpace(path1) && string.IsNullOrWhiteSpace(path2))
        {
            return true;
        }

        path1 = path1 is not null ? (path1.StartsWith("/") ? path1[1..] : path1) : string.Empty;
        path2 = path2 is not null ? (path2.StartsWith("/") ? path2[1..] : path2) : string.Empty;

        return path1.Equals(path2);
    }

    public static void Test()
    {
    }

}
