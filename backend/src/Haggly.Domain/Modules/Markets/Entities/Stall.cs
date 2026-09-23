using Haggly.Domain.Common;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Domain.Modules.Markets;

public sealed class Stall : SoftDeletableEntity
{
    public Guid MarketId { get; set; }
    public Guid VendorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LocationDescription { get; set; }
    public string? PhoneNumber { get; set; }
    public StallStatus Status { get; set; } = StallStatus.PENDING;

    public Market? Market { get; set; }
    public VendorProfile? Vendor { get; set; }
}
