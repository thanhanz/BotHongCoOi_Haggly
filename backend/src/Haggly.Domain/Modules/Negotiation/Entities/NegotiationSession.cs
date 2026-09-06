using Haggly.Domain.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Negotiation;

public sealed class NegotiationSession : AuditableEntity
{
    public Guid StallFulfillmentId { get; set; }
    public NegotiationSessionStatus Status { get; set; } = NegotiationSessionStatus.OPEN;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    public StallFulfillment? StallFulfillment { get; set; }
    public ICollection<NegotiationMessage> Messages { get; set; } = new List<NegotiationMessage>();
    public ICollection<NegotiationOffer> Offers { get; set; } = new List<NegotiationOffer>();
}
