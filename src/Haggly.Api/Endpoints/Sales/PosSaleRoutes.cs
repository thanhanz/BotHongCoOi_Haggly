using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Sales;

public static class PosSaleRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/vendor/stalls/{stallId:guid}/pos-sales";
    public const string Root = "";
}
