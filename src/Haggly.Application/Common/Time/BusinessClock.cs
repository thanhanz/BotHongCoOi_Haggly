namespace Haggly.Application.Common.Time;

public sealed class BusinessClock : IBusinessClock
{
    private readonly TimeProvider timeProvider;
    private readonly TimeZoneInfo businessTimeZone;

    public BusinessClock(TimeProvider timeProvider, TimeZoneInfo businessTimeZone)
    {
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.businessTimeZone = businessTimeZone ?? throw new ArgumentNullException(nameof(businessTimeZone));
    }

    public DateTimeOffset GetNow()
        => timeProvider.GetUtcNow().ToUniversalTime();

    public DateOnly GetBusinessDate()
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(GetNow(), businessTimeZone).DateTime);
}
