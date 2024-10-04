namespace Pango.Application.Common.Interfaces.Persistence;

public interface IHaveEncodedData
{
    /// <summary>
    /// File content
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Type of the data stored in the file: e.g. IEnumerable<Password> etc.
    /// </summary>
    public string? DataType { get; set; }
}
