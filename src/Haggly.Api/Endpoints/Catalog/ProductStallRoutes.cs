using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Catalog;

public static class ProductStallRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/stalls/{stallId:guid}/products";
    public const string ById = "/{id:guid}";
}
