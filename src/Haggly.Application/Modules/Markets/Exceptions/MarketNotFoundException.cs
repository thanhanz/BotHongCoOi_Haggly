namespace Haggly.Application.Modules.Markets.Exceptions;

public sealed class MarketNotFoundException(string message) : Exception(message);
