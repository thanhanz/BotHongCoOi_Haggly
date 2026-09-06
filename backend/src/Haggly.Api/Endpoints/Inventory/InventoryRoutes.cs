using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Inventory;

public static class InventoryRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/vendor/stalls/{stallId:guid}/inventory";
    public const string Root = "";
    public const string Items = "/items";
    public const string ItemById = "/items/{inventoryItemId:guid}";
    public const string Adjustments = "/adjustments";
    public const string Ledger = "/ledger";
}
