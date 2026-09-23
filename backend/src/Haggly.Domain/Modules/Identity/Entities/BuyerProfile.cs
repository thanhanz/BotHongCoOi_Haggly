using Haggly.Domain.Common;

namespace Haggly.Domain.Modules.Identity;

public sealed class BuyerProfile : AuditableRecord
{
    public Guid UserId { get; set; }
    public string? DefaultPickupNote { get; set; }
    public Guid? DefaultPaymentMethodId { get; set; }
}
