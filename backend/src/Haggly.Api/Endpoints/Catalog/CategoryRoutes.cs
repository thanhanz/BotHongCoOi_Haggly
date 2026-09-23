using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Catalog;

public static class CategoryRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/categories";
    public const string ById = "/{id:guid}";
}
