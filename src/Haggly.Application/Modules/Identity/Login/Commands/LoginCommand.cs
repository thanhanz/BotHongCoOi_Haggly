namespace Haggly.Application.Modules.Identity.Login.Commands;

public sealed record LoginCommand(
    string Email,
    string Password);
