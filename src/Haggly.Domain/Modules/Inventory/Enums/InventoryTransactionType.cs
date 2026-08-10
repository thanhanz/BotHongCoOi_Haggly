namespace Haggly.Domain.Modules.Inventory;

public enum InventoryTransactionType
{
    OPENING,
    POS_SALE,
    ORDER_RESERVE,
    RESERVATION_RELEASE,
    ONLINE_SALE,
    ADJUSTMENT,
    RETURN,
    PRICE_CHANGE
}
