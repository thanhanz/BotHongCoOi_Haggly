namespace Haggly.Api.Endpoints.Identity.Requests;

public sealed record LoginRequest(
    string Email,
    string Password);
