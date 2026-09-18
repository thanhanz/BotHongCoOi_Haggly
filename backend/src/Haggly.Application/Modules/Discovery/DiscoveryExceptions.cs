namespace Haggly.Application.Modules.Discovery;
public sealed class DiscoveryValidationException(string message) : Exception(message);
public sealed class DiscoveryNotFoundException(string message) : Exception(message);
