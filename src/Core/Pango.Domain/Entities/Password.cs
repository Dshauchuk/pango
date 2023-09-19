using Pango.Domain.Common;

namespace Pango.Domain.Entities;

/// <summary>
/// A password persisted in pango
/// </summary>
public class Password : BaseAuditableEntity
{
	public Password()
	{
		EncryptedValue = string.Empty;
		PasswordTarget = string.Empty;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="id">password id</param>
	/// <param name="ownerId">pango user id who ownes the password</param>
	/// <param name="encryptedValue">encrypted value of the password</param>
	/// <param name="passwordTarget">a resource that the password is for</param>
	public Password(Guid id, string encryptedValue, string passwordTarget)
	{
		Id = id;
		EncryptedValue = encryptedValue;
		PasswordTarget = passwordTarget;
	}

	/// <summary>
	/// Encrypted value of the password
	/// </summary>
	public string EncryptedValue { get; set; }

	/// <summary>
	/// A resource that the password is for
	/// </summary>
	public string PasswordTarget { get; set; }
}
