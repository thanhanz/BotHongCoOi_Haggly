namespace Haggly.Application.Modules.Sales.Exceptions;

public sealed class CartValidationException(string message) : Exception(message);
public sealed class CartNotFoundException(string message) : Exception(message);
public sealed class CartForbiddenException(string message) : Exception(message);
public sealed class CartConflictException(string message) : Exception(message);
