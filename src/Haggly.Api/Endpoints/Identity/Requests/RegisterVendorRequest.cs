namespace Haggly.Api.Endpoints.Identity.Requests;

public sealed record RegisterVendorRequest(
    string Email,
    string PhoneNumber,
    string Password,
    string FullName,
    string BusinessName,
    string? BusinessRegistrationNo = null,
    string? TaxCode = null);
