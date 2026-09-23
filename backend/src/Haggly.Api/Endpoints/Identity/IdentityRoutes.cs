namespace Haggly.Api.Endpoints.Identity;

public static class IdentityRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/identity";
    public const string RegisterBuyer = "/register/buyer";
    public const string RegisterVendor = "/register/vendor";
    public const string Login = "/login";
    public const string CurrentUser = "/me";
    public const string CurrentUserLocation = Prefix + CurrentUser;
}
