namespace Haggly.Application.Modules.Identity.Registration.Commands;

public sealed record RegisterVendorCommand(
    string Email,
    string PhoneNumber,
    string Password,
    string FullName,
    string BusinessName,
    string? BusinessRegistrationNo = null,
    string? TaxCode = null);
