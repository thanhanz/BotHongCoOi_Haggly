namespace Haggly.Application.Modules.Identity.Login.Exceptions;

public sealed class AuthenticationException(string message) : Exception(message);
