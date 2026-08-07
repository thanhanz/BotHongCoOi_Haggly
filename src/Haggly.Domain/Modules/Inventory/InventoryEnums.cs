namespace Haggly.Domain.Modules.Inventory;

public enum InventorySessionStatus
{
    Open,
    Closed,
    Reconciled
}

public enum DailyListingStatus
{
    Available,
    SoldOut,
    Hidden
}

public enum InventoryTransactionType
{
    Opening,
    PosSale,
    OrderReserve,
    ReservationRelease,
    OnlineSale,
    Adjustment,
    Return,
    PriceChange
}

public enum InventoryReservationStatus
{
    Active,
    Released,
    Consumed,
    Expired
}
