using Pango.Domain.Common;

namespace Pango.Domain.Entities;

/// <summary>
/// A Pango user
/// </summary>
public class User : BaseEntity
{
	public User()
	{
		UserName = string.Empty;
		MasterPasswordHash = string.Empty;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="id">user id</param>
	/// <param name="username">user name</param>
	public User(Guid id, string username)
		: base(id)
	{
		UserName = username;
		MasterPasswordHash = string.Empty;
	}

	public string UserName { get; set; }

	public string MasterPasswordHash { get; set; }
}
