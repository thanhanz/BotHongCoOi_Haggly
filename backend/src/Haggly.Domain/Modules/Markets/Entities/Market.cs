using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Markets;

public sealed class Market : SoftDeletableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? OpeningTime { get; set; }
    public TimeOnly? ClosingTime { get; set; }
    public MarketStatus Status { get; set; } = MarketStatus.ACTIVE;

    public ICollection<Stall> Stalls { get; set; } = new List<Stall>();
}
