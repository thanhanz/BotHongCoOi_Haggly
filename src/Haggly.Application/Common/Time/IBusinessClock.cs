namespace Haggly.Application.Common.Time;

public interface IBusinessClock
{
    DateTimeOffset GetNow();

    DateOnly GetBusinessDate();
}
