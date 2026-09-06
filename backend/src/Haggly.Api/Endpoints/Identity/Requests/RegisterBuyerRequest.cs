namespace Haggly.Api.Endpoints.Identity.Requests;

public sealed record RegisterBuyerRequest(
    string Email,
    string PhoneNumber,
    string Password,
    string FullName);
