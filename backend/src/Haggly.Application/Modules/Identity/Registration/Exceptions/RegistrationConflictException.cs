namespace Haggly.Application.Modules.Identity.Registration.Exceptions;

public sealed class RegistrationConflictException(string message) : Exception(message);
