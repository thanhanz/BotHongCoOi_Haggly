using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Sales;

public static class OrderRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/orders";
    public const string Root = "";
    public const string Detail = "/{orderId:guid}";
    public const string Cancel = "/{orderId:guid}/cancel";
}
