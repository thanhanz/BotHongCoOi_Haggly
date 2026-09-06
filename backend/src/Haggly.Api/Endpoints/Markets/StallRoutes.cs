using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Markets;

public static class StallRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/markets/stalls";
    public const string ById = "/{id:guid}";
}
