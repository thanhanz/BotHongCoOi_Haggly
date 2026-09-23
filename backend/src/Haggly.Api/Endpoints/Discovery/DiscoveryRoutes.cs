using Haggly.Api.Endpoints;
namespace Haggly.Api.Endpoints.Discovery;
public static class DiscoveryRoutes
{
    public const string CommonDishes = ApiRoutes.Version1 + "/common-dishes";
    public const string Search = "/search";
    public const string Proposal = "/{dishId:guid}/proposal";
    public const string CanonicalIngredients = ApiRoutes.Version1 + "/canonical-ingredients";
    public const string ProductMapping = ApiRoutes.Version1 + "/products/{productId:guid}/canonical-ingredient";
}
