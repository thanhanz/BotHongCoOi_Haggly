using Haggly.Domain.Common;
using Haggly.Domain.Modules.Sales;

namespace Haggly.Domain.Modules.Negotiation;

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
