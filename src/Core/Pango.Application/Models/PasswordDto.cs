namespace Pango.Application.Models;

/// <summary>
/// 
/// </summary>
public class PasswordDto
{
    public PasswordDto()
    {
        Value = string.Empty;
        Target = string.Empty;
        UserName = string.Empty;
        Name = string.Empty;
        Login = string.Empty;
        Properties = new();
    }

    /// <summary>
    /// A password title
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// User's login for the resource
    /// </summary>
    public string Login { get; set; }

    /// <summary>
    /// Password entry properties
    /// </summary>
    public Dictionary<string, string> Properties { get; set; }

    /// <summary>
    /// Encrypted value of the password
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// A resource that the password is for
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Name of the password owner
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset LastModifiedAt { get; set; }
}
