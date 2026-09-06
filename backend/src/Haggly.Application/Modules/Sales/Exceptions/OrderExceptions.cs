namespace Haggly.Application.Modules.Sales.Exceptions;

public sealed class OrderValidationException(string message) : Exception(message);
public sealed class OrderNotFoundException(string message) : Exception(message);
public sealed class OrderForbiddenException(string message) : Exception(message);
public sealed class OrderConflictException(string message) : Exception(message);
