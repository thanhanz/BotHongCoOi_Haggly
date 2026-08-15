using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Inventory;

public static class InventoryRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/vendor/stalls/{stallId:guid}";
    public const string OpenSession = "/inventory-sessions/open";
    public const string CurrentSession = "/inventory-sessions/current";
    public const string PreviousSession = "/inventory-sessions/previous";
    public const string CloseSession = "/inventory-sessions/current/close";
    public const string Listings = "/inventory-listings";
    public const string ListingById = "/inventory-listings/{listingId:guid}";
    public const string Adjustments = "/inventory-adjustments";
    public const string Ledger = "/inventory-ledger";
}
