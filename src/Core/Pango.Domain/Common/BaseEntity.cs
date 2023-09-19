namespace Pango.Domain.Common;

public abstract class BaseEntity
{
	public BaseEntity()
	{

	}

	public BaseEntity(Guid id)
	{
		Id = id;
	}

    public Guid Id { get; set; }
}
