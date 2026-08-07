using Haggly.Domain.Common;
using Haggly.Domain.Modules.Identity;

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
    public MarketStatus Status { get; set; } = MarketStatus.Active;

    public ICollection<Stall> Stalls { get; set; } = new List<Stall>();
}

public sealed class Stall : SoftDeletableEntity
{
    public Guid MarketId { get; set; }
    public Guid VendorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LocationDescription { get; set; }
    public string? PhoneNumber { get; set; }
    public StallStatus Status { get; set; } = StallStatus.Pending;

    public Market? Market { get; set; }
    public VendorProfile? Vendor { get; set; }
}
