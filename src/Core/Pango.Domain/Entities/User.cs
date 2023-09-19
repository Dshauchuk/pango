using Pango.Domain.Common;

namespace Pango.Domain.Entities;

/// <summary>
/// A Pango user
/// </summary>
public class User : BaseEntity
{
	public User()
	{
		Username = string.Empty;
		MasterKeyHash = string.Empty;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="id">user id</param>
	/// <param name="username">user name</param>
	public User(Guid id, string username)
		: base(id)
	{
		Username = username;
		MasterKeyHash = string.Empty;
	}

	public string Username { get; set; }

	public string MasterKeyHash { get; set; }
}
