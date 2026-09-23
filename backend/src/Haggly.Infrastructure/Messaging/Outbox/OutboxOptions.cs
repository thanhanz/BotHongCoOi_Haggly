namespace Haggly.Infrastructure.Messaging.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; init; }
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; init; } = 100;

    public bool IsValid()
        => !Enabled || (Interval > TimeSpan.Zero && BatchSize > 0);
}
