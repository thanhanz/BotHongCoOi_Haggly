namespace Haggly.Application.Modules.Markets.Exceptions.Markets;

public sealed class MarketConflictException(string message) : Exception(message);
