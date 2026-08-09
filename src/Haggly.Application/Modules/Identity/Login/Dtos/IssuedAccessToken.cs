namespace Haggly.Application.Modules.Identity.Login.Dtos;

public sealed record IssuedAccessToken(
    string Value,
    DateTimeOffset ExpiresAt);
