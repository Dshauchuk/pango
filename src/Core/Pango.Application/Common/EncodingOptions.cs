namespace Pango.Application.Common;

public readonly record struct EncodingOptions(string key, string salt)
{
    public string Key { get; } = key;

    public string Salt { get; } = salt;
}
