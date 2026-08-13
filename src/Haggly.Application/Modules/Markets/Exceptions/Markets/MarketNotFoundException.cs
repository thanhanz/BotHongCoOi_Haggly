namespace Haggly.Application.Modules.Markets.Exceptions.Markets;

public sealed class MarketNotFoundException(string message) : Exception(message);
