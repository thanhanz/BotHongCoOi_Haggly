using Haggly.Domain.Common;
using Haggly.Domain.Modules.Identity;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Negotiation;

public sealed class NegotiationSession : AuditableEntity
{
    public Guid StallFulfillmentId { get; set; }
    public NegotiationSessionStatus Status { get; set; } = NegotiationSessionStatus.Open;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    public StallFulfillment? StallFulfillment { get; set; }
    public ICollection<NegotiationMessage> Messages { get; set; } = new List<NegotiationMessage>();
    public ICollection<NegotiationOffer> Offers { get; set; } = new List<NegotiationOffer>();
}

public sealed class NegotiationMessage : ImmutableEntity
{
    public Guid NegotiationSessionId { get; set; }
    public Guid SenderUserId { get; set; }
    public NegotiationMessageType MessageType { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }

    public NegotiationSession? NegotiationSession { get; set; }
    public User? SenderUser { get; set; }
}

public sealed class NegotiationOffer : AuditableEntity
{
    public Guid NegotiationSessionId { get; set; }
    public int OfferNo { get; set; }
    public Guid OfferedByUserId { get; set; }
    public NegotiationOfferType OfferType { get; set; }
    public NegotiationOfferStatus Status { get; set; } = NegotiationOfferStatus.Pending;
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

public sealed class NegotiationOfferItem : ImmutableEntity
{
    public Guid NegotiationOfferId { get; set; }
    public Guid OrderItemId { get; set; }
    public decimal ProposedQuantity { get; set; }
    public decimal ProposedUnitPrice { get; set; }
    public decimal ProposedLineTotal { get; set; }

    public NegotiationOffer? NegotiationOffer { get; set; }
    public OrderItem? OrderItem { get; set; }
}
