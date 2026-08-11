namespace Haggly.Application.Modules.Markets.Exceptions;

public sealed class MarketConflictException(string message) : Exception(message);
