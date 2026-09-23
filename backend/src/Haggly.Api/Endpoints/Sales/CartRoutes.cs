using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Sales;

public static class CartRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/cart";
    public const string Root = "";
    public const string Items = "/items";
    public const string ItemById = "/items/{cartItemId:guid}";
    public const string Checkout = "/checkout";
}
