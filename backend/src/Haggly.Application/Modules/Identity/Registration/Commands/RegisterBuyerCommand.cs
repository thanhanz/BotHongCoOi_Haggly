namespace Haggly.Application.Modules.Identity.Registration.Commands;

public sealed record RegisterBuyerCommand(
    string Email,
    string PhoneNumber,
    string Password,
    string FullName);
