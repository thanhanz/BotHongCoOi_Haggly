using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Catalog;

public static class ProductRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/products";
    public const string ById = "/{id:guid}";
}
