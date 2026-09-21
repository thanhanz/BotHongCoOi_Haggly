namespace Haggly.Infrastructure.Messaging.Outbox;

public sealed class OutboxBenchmarkOptions
{
    public const string SectionName = "OutboxBenchmark";

    public bool Enabled { get; init; }
    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(1);

    public bool IsValid()
        => !Enabled || Duration > TimeSpan.Zero;
}
