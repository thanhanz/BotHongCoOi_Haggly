namespace Haggly.Application.Modules.Markets.Exceptions;

public sealed class MarketValidationException(string message) : Exception(message);
