namespace Haggly.Api.Endpoints.Identity.Responses;

public sealed record CurrentUserResponse(
    Guid UserId,
    string? Email,
    IReadOnlyCollection<string> Roles);
