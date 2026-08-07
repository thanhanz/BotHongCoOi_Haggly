namespace Haggly.Domain.Common;

public abstract class ImmutableEntity : Entity
{
    protected ImmutableEntity()
    {
    }

    protected ImmutableEntity(Guid id)
        : base(id)
    {
    }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
}
