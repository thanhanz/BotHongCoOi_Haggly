namespace Haggly.Domain.Common;

public abstract class Entity
{
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; protected set; }
}
