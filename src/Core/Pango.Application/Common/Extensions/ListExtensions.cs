namespace Pango.Application.Common.Extensions;

public static class ListExtensions
{
    /// <summary>
    /// Splits a list into smaller lists of a specified size without heavy LINQ allocations.
    /// </summary>
    public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
    {
        if (chunkSize <= 0) throw new ArgumentException("Chunk size must be greater than zero.", nameof(chunkSize));

        var result = new List<List<T>>((source.Count + chunkSize - 1) / chunkSize);
        for (int i = 0; i < source.Count; i += chunkSize)
        {
            result.Add(source.GetRange(i, Math.Min(chunkSize, source.Count - i)));
        }
        return result;
    }
}
