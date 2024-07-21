namespace Pango.Application.Common;

public readonly record struct EncodingOptions
{
    public EncodingOptions(string key, string salt) : this()
    {
        Key = key;
        Salt = salt;
    }

    public string Key { get; }

    public string Salt { get; }
}
