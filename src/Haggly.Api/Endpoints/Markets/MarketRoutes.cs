using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Markets;

public static class MarketRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/markets";
    public const string ById = "/{id:guid}";
}
