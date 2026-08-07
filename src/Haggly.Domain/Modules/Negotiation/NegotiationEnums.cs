namespace Haggly.Domain.Modules.Negotiation;

public enum NegotiationSessionStatus
{
    Open,
    Agreed,
    Expired,
    Cancelled
}

public enum NegotiationMessageType
{
    Text,
    System,
    OfferReference
}

public enum NegotiationOfferType
{
    BuyerOffer,
    VendorCounteroffer
}

public enum NegotiationOfferStatus
{
    Pending,
    Accepted,
    Rejected,
    Superseded,
    Expired
}
