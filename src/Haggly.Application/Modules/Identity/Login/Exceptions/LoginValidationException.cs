namespace Haggly.Application.Modules.Identity.Login.Exceptions;

public sealed class LoginValidationException(string message) : Exception(message);
