namespace Haggly.Api.Authorization;

public static class IdentityPolicies
{
    public const string BuyerOnly = "identity:buyer";
    public const string VendorOnly = "identity:vendor";
    public const string AdminOnly = "identity:admin";
}
