using Haggly.Domain.Common;
using Haggly.Domain.Modules.Identity;

namespace Haggly.Domain.Modules.Negotiation;

public sealed class NegotiationOffer : AuditableEntity
{
    public Guid NegotiationSessionId { get; set; }
    public int OfferNo { get; set; }
    public Guid OfferedByUserId { get; set; }
    public NegotiationOfferType OfferType { get; set; }
    public NegotiationOfferStatus Status { get; set; } = NegotiationOfferStatus.PENDING;
    public Guid? MessageId { get; set; }
    public DateTimeOffset OfferedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public Guid? RespondedByUserId { get; set; }

    public NegotiationSession? NegotiationSession { get; set; }
    public User? OfferedByUser { get; set; }
    public NegotiationMessage? Message { get; set; }
    public User? RespondedByUser { get; set; }
    public ICollection<NegotiationOfferItem> Items { get; set; } = new List<NegotiationOfferItem>();
}
