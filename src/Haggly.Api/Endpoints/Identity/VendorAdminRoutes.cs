using Haggly.Api.Endpoints;

namespace Haggly.Api.Endpoints.Identity;

public static class VendorAdminRoutes
{
    public const string Prefix = ApiRoutes.Version1 + "/admin/vendors";
    public const string ById = "/{vendorId:guid}";
    public const string Approve = ById + "/approve";
    public const string Reject = ById + "/reject";
    public const string Suspend = ById + "/suspend";
}
